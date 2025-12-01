import { useState, useEffect } from "react";
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
import { type NotificationWeb, NotificationType } from "../types/notification.types";

export default function NotificationBell() {
  const [notifications, setNotifications] = useState<NotificationWeb[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [isOpen, setIsOpen] = useState(false);

  const bgColor = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");
  const unreadBg = useColorModeValue("blue.50", "blue.900");

  // Pobierz powiadomienia tylko gdy użytkownik kliknie w dzwoneczek
  const fetchNotifications = async () => {
    setLoading(true);
    try {
      const response = await notificationApi.getUnreadNotifications();
      if (response.ok) {
        const data: NotificationWeb[] = await response.json();
        setNotifications(data);
        setUnreadCount(data.filter(n => !n.readed).length);
      }
    } catch (error) {
      console.error("Błąd pobierania powiadomień:", error);
    } finally {
      setLoading(false);
    }
  };

  // Pobierz licznik nieprzeczytanych powiadomień przy montowaniu
  useEffect(() => {
    const fetchUnreadCount = async () => {
      try {
        const response = await notificationApi.getUnreadNotifications();
        if (response.ok) {
          const data: NotificationWeb[] = await response.json();
          setUnreadCount(data.filter(n => !n.readed).length);
        }
      } catch (error) {
        console.error("Błąd pobierania licznika powiadomień:", error);
      }
    };

    fetchUnreadCount();
  }, []);

  // Połączenie SignalR i nasłuchiwanie na nowe powiadomienia - TYLKO RAZ
  useEffect(() => {
    let unsubscribe: (() => void) | null = null;

    const initSignalR = async () => {
      try {
        // Uruchom połączenie SignalR
        await notificationHubService.startConnection();

        // Subskrybuj na nowe powiadomienia
        unsubscribe = notificationHubService.onNotificationReceived(async (notification) => {
          console.log("Nowe powiadomienie w dzwoneczku:", notification);
          
          // Pobierz świeżą listę nieprzeczytanych powiadomień z API
          try {
            const response = await notificationApi.getUnreadNotifications();
            if (response.ok) {
              const data: NotificationWeb[] = await response.json();
              setNotifications(data);
              setUnreadCount(data.filter(n => !n.readed).length);
            }
          } catch (error) {
            console.error("Błąd odświeżania powiadomień:", error);
            // Fallback - zwiększ licznik lokalnie
            setUnreadCount(prev => prev + 1);
            setNotifications(prev => [notification, ...prev]);
          }
        });
      } catch (error) {
        console.error("Błąd inicjalizacji SignalR:", error);
      }
    };

    initSignalR();

    // Cleanup przy unmount - usuń listener
    return () => {
      if (unsubscribe) {
        unsubscribe();
      }
    };
  }, []); // ⚠️ Pusta tablica zależności - uruchom TYLKO RAZ przy mount

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
      const response = await notificationApi.markAsRead(notificationId);
      console.log("🔵 Odpowiedź API:", response.status, response.ok);
      
      if (response.ok) {
        console.log("✅ Oznaczono jako przeczytane, odświeżam listę...");
        // Odśwież listę powiadomień z API
        const refreshResponse = await notificationApi.getUnreadNotifications();
        if (refreshResponse.ok) {
          const data: NotificationWeb[] = await refreshResponse.json();
          console.log("✅ Pobrano odświeżoną listę:", data.length, "powiadomień");
          setNotifications(data);
          setUnreadCount(data.filter(n => !n.readed).length);
        }
      } else {
        const errorText = await response.text();
        console.error("❌ Błąd oznaczania jako przeczytane - status:", response.status, errorText);
      }
    } catch (error) {
      console.error("❌ Błąd oznaczania powiadomienia jako przeczytane:", error);
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
        fetchNotifications();
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
                      
                      <Text fontSize="sm" color={useColorModeValue("gray.700", "gray.300")} noOfLines={2} lineHeight="1.4">
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
