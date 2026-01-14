import { useState, useEffect } from "react";
import {
  Box,
  Badge,
  Button,
  Divider,
  HStack,
  Icon,
  IconButton,
  Popover,
  PopoverTrigger,
  PopoverContent,
  PopoverHeader,
  PopoverBody,
  Spinner,
  Tab,
  TabList,
  TabPanel,
  TabPanels,
  Tabs,
  Text,
  VStack,
  useColorModeValue,
  useToast,
} from "@chakra-ui/react";
import { Bell, Info, CheckCircle, AlertTriangle, XCircle } from "lucide-react";
import { notificationApi } from "../api/notificationApi";
import { notificationHubService } from "../services/notificationHubService";
import { handleApiError } from "../utils/handleApiError";
import { type NotificationWeb, NotificationType } from "../types/notification.types";

export default function NotificationBell() {
  const [allNotifications, setAllNotifications] = useState<NotificationWeb[]>([]);
  const [unreadNotifications, setUnreadNotifications] = useState<NotificationWeb[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [isOpen, setIsOpen] = useState(false);
  const [activeTab, setActiveTab] = useState(0);
  const toast = useToast();

  const bgColor = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");
  const unreadBg = useColorModeValue("blue.50", "blue.900");
  const messageTextColor = useColorModeValue("gray.700", "gray.300");

  const fetchUnreadCounter = async () => {
    try {
      const count = await notificationApi.getUnreadCounter();
      setUnreadCount(count);
      console.log("🔵 Unread counter fetched:", count);
    } catch (error) {
      console.error("Błąd pobierania licznika:", error);
    }
  };

  // Inicjalizacja - pobierz licznik nieprzeczytanych
  useEffect(() => {
    fetchUnreadCounter();
  }, []);

  // SignalR - nasłuchuj na nowe powiadomienia
  useEffect(() => {
    const unsubscribeNew = notificationHubService.onNotificationReceived((payload) => {
      console.log("🔔 [UI] Otrzymano powiadomienie:", {
        title: payload.notification.title,
        oldCounter: unreadCount,
        newCounter: payload.unreadNotificationCounter,
      });
      
      // Zaktualizuj licznik z backendu (snapshot)
      setUnreadCount(payload.unreadNotificationCounter);
      
      // Pokaż toast
      toast({
        title: payload.notification.title,
        description: payload.notification.message,
        status: getToastStatus(payload.notification.type),
        duration: 5000,
        isClosable: true,
        position: "top-right",
      });
      
      console.log("✅ [UI] Licznik zaktualizowany na:", payload.unreadNotificationCounter);
    });

    const unsubscribeSync = notificationHubService.onNotificationSynced(async () => {
      console.log("🔄 [UI] Synchronizacja - odśwież licznik");
      await fetchUnreadCounter();
    });

    return () => {
      unsubscribeNew();
      unsubscribeSync();
    };
  }, []);

  // Załaduj powiadomienia przy otwarciu popover i zmianie taba
  useEffect(() => {
    if (isOpen) {
      // Pobierz aktualny licznik nieprzeczytanych
      fetchUnreadCounter();
      // Załaduj listę powiadomień
      loadNotifications();
    }
  }, [isOpen, activeTab]);

  const loadNotifications = async () => {
    setLoading(true);
    try {
      if (activeTab === 0) {
        // Tab "Wszystkie"
        const notifications = await notificationApi.getAll(50, 0);
        setAllNotifications(notifications);
      } else {
        // Tab "Nieprzeczytane"
        const notifications = await notificationApi.getUnread(50, 0);
        setUnreadNotifications(notifications);
      }
    } catch (error) {
      const { title, description } = handleApiError(error);
      console.error("❌ Błąd ładowania powiadomień:", title, description);
      toast({
        title,
        description,
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    } finally {
      setLoading(false);
    }
  };

  const handleMarkAsRead = async (notificationId: string) => {
    try {
      await notificationApi.markAsRead(notificationId);
      
      // Odśwież licznik z API
      await fetchUnreadCounter();
      
      // Przeładuj aktualne powiadomienia
      await loadNotifications();
      
      console.log("✅ Powiadomienie oznaczone jako przeczytane");
    } catch (error) {
      const { title, description } = handleApiError(error);
      console.error("❌ Błąd oznaczania powiadomienia:", title, description);
      toast({
        title,
        description,
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    }
  };

  const handleMarkAllAsRead = async () => {
    try {
      const result = await notificationApi.markAllAsRead();
      
      if (result.markedCount > 0) {
        // Refresh notifications and counter
        await Promise.all([
          loadNotifications(),
          fetchUnreadCounter(),
        ]);
        
        toast({
          title: "Oznaczono wszystkie jako przeczytane",
          description: `Oznaczono ${result.markedCount} powiadomień`,
          status: "success",
          duration: 3000,
          isClosable: true,
        });
      }
    } catch (error) {
      console.error("Failed to mark all as read:", error);
      const { title, description } = handleApiError(error);
      toast({
        title,
        description,
        status: "error",
        duration: 5000,
        isClosable: true,
      });
    }
  };

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

  const getToastStatus = (type: NotificationType): "success" | "warning" | "error" | "info" => {
    switch (type) {
      case NotificationType.Success: return "success";
      case NotificationType.Warning: return "warning";
      case NotificationType.Error: return "error";
      default: return "info";
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

  const renderNotifications = (notifications: NotificationWeb[]) => (
    loading ? (
      <Box textAlign="center" py={8}>
        <Spinner size="lg" color="blue.500" />
      </Box>
    ) : notifications.length === 0 ? (
      <Box textAlign="center" py={8}>
        <Icon as={Bell} boxSize={12} color="gray.400" mb={2} />
        <Text color="gray.500" fontSize="sm">
          Brak powiadomień
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
    )
  );

  return (
    <Popover
      isOpen={isOpen}
      onOpen={() => setIsOpen(true)}
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
            <HStack spacing={2}>
              {unreadCount > 0 && (
                <Badge colorScheme="blue">{unreadCount} nowych</Badge>
              )}
              {unreadCount > 0 && (
                <Button
                  size="xs"
                  variant="ghost"
                  colorScheme="blue"
                  onClick={handleMarkAllAsRead}
                  leftIcon={<Icon as={CheckCircle} boxSize={3} />}
                >
                  Oznacz wszystkie
                </Button>
              )}
            </HStack>
          </HStack>
        </PopoverHeader>
        <Divider />
        <Tabs index={activeTab} onChange={setActiveTab}>
          <TabList>
            <Tab>Wszystkie</Tab>
            <Tab>Nieprzeczytane</Tab>
          </TabList>
          <TabPanels>
            <TabPanel p={0}>
              <Box maxH="400px" overflowY="auto">
                {renderNotifications(allNotifications)}
              </Box>
            </TabPanel>
            <TabPanel p={0}>
              <Box maxH="400px" overflowY="auto">
                {renderNotifications(unreadNotifications)}
              </Box>
            </TabPanel>
          </TabPanels>
        </Tabs>
      </PopoverContent>
    </Popover>
  );
}
