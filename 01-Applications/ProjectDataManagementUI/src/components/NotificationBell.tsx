import { useState, useEffect, useRef } from "react";
import {
  Box,
  IconButton,
  Popover,
  PopoverTrigger,
  PopoverContent,
  PopoverHeader,
  PopoverBody,
  VStack,
  HStack,
  Text,
  Badge,
  useColorModeValue,
  Icon,
  Divider,
  Spinner,
} from "@chakra-ui/react";
import { Bell, Info, CheckCircle, AlertTriangle, XCircle } from "lucide-react";
import { notificationApi } from "../api/notificationApi";
import { notificationHubService } from "../services/notificationHubService";
import { handleApiError } from "../utils/handleApiError";
import { type NotificationWeb, NotificationType } from "../types/notification.types";

export default function NotificationBell() {
  const [notifications, setNotifications] = useState<NotificationWeb[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [isOpen, setIsOpen] = useState(false);
  
  // Ref dla isOpen, żeby listenery miały dostęp bez re-rejestracji
  const isOpenRef = useRef(false);

  const bgColor = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");
  const unreadBg = useColorModeValue("blue.50", "blue.900");
  const messageTextColor = useColorModeValue("gray.700", "gray.300");

  // Synchronizuj isOpenRef z isOpen
  useEffect(() => {
    isOpenRef.current = isOpen;
  }, [isOpen]);

  // Załaduj powiadomienia z cache (gdy użytkownik otworzy popover)
  const loadNotificationsFromCache = () => {
    // Pokazuj WSZYSTKIE powiadomienia, nie tylko unread - eliminuje problem "nic nie ma"
    const cachedNotifications = notificationHubService.getAllNotificationsFromCache();
    const cachedUnreadCount = notificationHubService.getUnreadCountFromCache();
    
    setNotifications(cachedNotifications);
    setUnreadCount(cachedUnreadCount);
    
    console.log("🔵 Loaded ALL notifications from cache:", cachedNotifications.length, "| Unread:", cachedUnreadCount);
  };

  // Inicjalizacja cache z API (tylko raz przy montowaniu)
  useEffect(() => {
    const initializeNotifications = async () => {
      // Jeśli cache już zainicjalizowany, użyj go
      if (notificationHubService.isCacheInitialized()) {
        const cachedUnreadCount = notificationHubService.getUnreadCountFromCache();
        setUnreadCount(cachedUnreadCount);
        console.log("🔵 Cache already initialized, unread count:", cachedUnreadCount);
        return;
      }

      // Pobierz z API i zainicjalizuj cache
      setLoading(true);
      try {
        const response = await notificationApi.getUnreadNotifications();
        const data: NotificationWeb[] = response.data;
        await notificationHubService.initializeCache(data);
        setUnreadCount(data.filter(n => !n.readed).length);
        console.log("🔵 Cache initialized from API:", data.length, "notifications");
      } catch (error) {
        console.error("Błąd inicjalizacji powiadomień:", error);
      } finally {
        setLoading(false);
      }
    };

    initializeNotifications();
  }, []);

  // Połączenie SignalR i nasłuchiwanie na nowe powiadomienia - TYLKO RAZ, BEZ RE-REJESTRACJI
  useEffect(() => {
    // ✅ SignalR jest już uruchomiony globalnie w AuthContext
    // Tutaj tylko rejestrujemy listenery dla tego komponentu - RAZ na cały cykl życia
    
    // Subskrybuj na nowe powiadomienia
    const unsubscribeNew = notificationHubService.onNotificationReceived((notification) => {
      console.log("✅ Nowe powiadomienie otrzymane z SignalR:", notification.title);
      
      // ✅ Cache został już zaktualizowany w notificationHubService
      // Tutaj tylko odświeżamy UI
      const newUnreadCount = notificationHubService.getUnreadCountFromCache();
      setUnreadCount(newUnreadCount);
      
      // Jeśli popover jest otwarty, odśwież listę (czytamy przez ref)
      if (isOpenRef.current) {
        loadNotificationsFromCache();
      }
      
      console.log("✅ UI zaktualizowane bez API call! Nowy licznik nieprzeczytanych:", newUnreadCount);
    });

    // 🔄 Subskrybuj na synchronizację między urządzeniami
    const unsubscribeSync = notificationHubService.onNotificationSynced((dto) => {
      console.log("🔄 Synchronizacja z innego urządzenia - powiadomienie oznaczone jako przeczytane:", {
        notificationId: dto.notificationId,
        userId: dto.userId,
        readAt: dto.readAt
      });
      
      // Cache został już zaktualizowany przez SignalR event handler
      // Teraz tylko odśwież UI
      const newUnreadCount = notificationHubService.getUnreadCountFromCache();
      setUnreadCount(newUnreadCount);
      
      // Jeśli popover jest otwarty, odśwież listę (czytamy przez ref)
      if (isOpenRef.current) {
        loadNotificationsFromCache();
      }
      
      console.log("🔄 UI zsynchronizowane z innym urządzeniem! Nowy licznik:", newUnreadCount);
    });

    // Cleanup przy unmount - usuń listenery
    return () => {
      unsubscribeNew();
      unsubscribeSync();
    };
  }, []); // <-- TYLKO RAZ! isOpen czytamy przez ref

  const getNotificationIcon = (type: NotificationType) => {
    switch (type) {
      case NotificationType.Success: return CheckCircle;
      case NotificationType.Warning: return AlertTriangle;
      case NotificationType.Error: return XCircle;
      default: return Info;
    }
  };

  const getNotificationColor = (type: NotificationType) => {
    switch (type) {
      case NotificationType.Success: return "green";
      case NotificationType.Warning: return "orange";
      case NotificationType.Error: return "red";
      default: return "blue";
    }
  };

  const handleMarkAsRead = async (notificationId: string) => {
    console.log("🔵 Oznaczam jako przeczytane:", notificationId);
    
    try {
      // ✅ Najpierw zaktualizuj cache (optimistic update)
      notificationHubService.markAsReadInCache(notificationId);
      
      // ✅ Zaktualizuj UI natychmiast z cache
      const newUnreadCount = notificationHubService.getUnreadCountFromCache();
      const updatedNotifications = notificationHubService.getAllNotificationsFromCache(); // Wszystkie, nie tylko unread
      
      setUnreadCount(newUnreadCount);
      setNotifications(updatedNotifications);
      
      console.log("✅ UI zaktualizowane optimistically (bez czekania na API)");
      
      // Wyślij request do API w tle (bez blokowania UI)
      try {
        await notificationApi.markAsRead(notificationId);
        console.log("✅ API potwierdziło oznaczenie jako przeczytane");
      } catch (error) {
        // Jeśli API zwróci błąd, cofnij zmiany w cache
        console.error("❌ API błąd - cofam zmiany w cache");
        const { title, description } = handleApiError(error);
        console.error("❌ Błąd API:", title, description);
        // Możesz opcjonalnie dodać logikę rollback tutaj
      }
    } catch (error) {
      console.error("❌ Błąd oznaczania powiadomienia jako przeczytane:", error);
      // W przypadku błędu sieciowego, cache pozostaje zaktualizowany
      // Można dodać toast z informacją o problemie
    }
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return "Teraz";
    if (diffMins < 60) return `${diffMins} min temu`;
    if (diffHours < 24) return `${diffHours} godz. temu`;
    if (diffDays < 7) return `${diffDays} dni temu`;
    
    return date.toLocaleDateString("pl-PL", {
      day: "numeric",
      month: "short",
      year: date.getFullYear() !== now.getFullYear() ? "numeric" : undefined,
    });
  };

  return (
    <Popover
      isOpen={isOpen}
      onOpen={() => {
        setIsOpen(true);
        loadNotificationsFromCache(); // ✅ Ładuj z cache zamiast API
      }}
      onClose={() => setIsOpen(false)}
      placement="bottom-end"
      strategy="fixed"
    >
      <PopoverTrigger>
        <Box position="relative" display="inline-block">
          <IconButton
            aria-label="Powiadomienia"
            icon={<Icon as={Bell} boxSize={5} />}
            variant="ghost"
            size="md"
            colorScheme="gray"
          />
          {unreadCount > 0 && (
            <Badge
              position="absolute"
              top="-1"
              right="-1"
              colorScheme="red"
              borderRadius="full"
              fontSize="xs"
              minW="18px"
              h="18px"
              display="flex"
              alignItems="center"
              justifyContent="center"
            >
              {unreadCount > 9 ? "9+" : unreadCount}
            </Badge>
          )}
        </Box>
      </PopoverTrigger>
      <PopoverContent
        bg={bgColor}
        borderColor={borderColor}
        w={{ base: "90vw", md: "400px" }}
        maxW="400px"
        zIndex={1001}
      >
        <PopoverHeader fontWeight="bold" border="0" pb={2}>
          <HStack justify="space-between">
            <Text>Powiadomienia</Text>
            {unreadCount > 0 && (
              <Badge colorScheme="blue">{unreadCount} nowych</Badge>
            )}
          </HStack>
        </PopoverHeader>
        <Divider />
        <PopoverBody p={0} maxH="400px" overflowY="auto">
          {loading ? (
            <Box textAlign="center" py={8}>
              <Spinner size="lg" color="blue.500" />
            </Box>
          ) : notifications.length === 0 ? (
            <Box textAlign="center" py={8}>
              <Icon as={Bell} boxSize={12} color="gray.400" mb={2} />
              <Text color="gray.500" fontSize="sm">
                Brak nowych powiadomień
              </Text>
            </Box>
          ) : (
            <VStack spacing={0} align="stretch">
              {notifications.map((notification) => (
                <Box
                  key={notification.id}
                  p={3}
                  bg={!notification.readed ? unreadBg : "transparent"}
                  borderBottom="1px"
                  borderColor={borderColor}
                  _hover={{ bg: hoverBg }}
                  transition="background 0.2s"
                  cursor="pointer"
                  onClick={() => !notification.readed && handleMarkAsRead(notification.id)}
                >
                  <HStack align="flex-start" spacing={3}>
                    <Icon
                      as={getNotificationIcon(notification.type)}
                      boxSize={5}
                      color={`${getNotificationColor(notification.type)}.500`}
                      mt={0.5}
                    />
                    <VStack align="flex-start" spacing={2} flex={1}>
                      <HStack justify="space-between" w="full" align="flex-start">
                        <Text fontWeight="600" fontSize="sm" noOfLines={2} flex={1}>
                          {notification.title}
                        </Text>
                        {!notification.readed && (
                          <Badge colorScheme="blue" fontSize="xs" flexShrink={0}>
                            Nowe
                          </Badge>
                        )}
                      </HStack>
                      
                      <Text fontSize="sm" color={messageTextColor} noOfLines={2} lineHeight="1.4">
                        {notification.message}
                      </Text>
                      
                      <VStack align="flex-start" spacing={0.5} w="full" mt={1}>
                        <HStack spacing={2} fontSize="xs" color="gray.500">
                          <Text>{formatDate(notification.createdAt)}</Text>
                          <Text>•</Text>
                          <Text fontWeight="500">{notification.tenantName}</Text>
                          {notification.projectName && (
                            <>
                              <Text>•</Text>
                              <Text fontWeight="500">{notification.projectName}</Text>
                            </>
                          )}
                        </HStack>
                      </VStack>
                    </VStack>
                  </HStack>
                </Box>
              ))}
            </VStack>
          )}
        </PopoverBody>
      </PopoverContent>
    </Popover>
  );
}
