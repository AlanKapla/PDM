import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
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
} from "@chakra-ui/react";
import { Bell, Info, CheckCircle, AlertTriangle, XCircle } from "lucide-react";
import { useQueryClient } from "@tanstack/react-query";
import {
  useUnreadCounter,
  useNotificationsInfinite,
  useMarkAsRead,
  useMarkAllAsRead,
  notificationKeys,
  aiCostImportKeys,
} from "../hooks/queries";
import { useToastNotification } from "../hooks/useToastNotification";
import { notificationHubService } from "../services/notificationHubService";
import { type NotificationWeb, NotificationType, type NotificationMetadata } from "../types/notification.types";
import { getRelativeTime } from "../utils/formatters";

function resolveNotificationRoute(metadata: NotificationMetadata | undefined): string | undefined {
  if (!metadata?.route) {
    return undefined;
  }

  const contextualMatch = metadata.route.match(
    /\/projects\/([^/]+)\/(costs|dashboard)\/ai-review/
  );
  if (contextualMatch) {
    return `/projects/${contextualMatch[1]}/${contextualMatch[2]}/ai-review`;
  }

  if (metadata.route.startsWith('/projects/')) {
    return metadata.route;
  }
  return metadata.route;
}

function isAICostImportNotification(metadata: NotificationMetadata | undefined): boolean {
  if (!metadata) {
    return false;
  }
  return Boolean(metadata.batchId || metadata.route?.includes("ai-review"));
}

export default function NotificationBell() {
  const [isOpen, setIsOpen] = useState(false);
  const [activeTab, setActiveTab] = useState(0);
  const navigate = useNavigate();
  const { showSuccess, showError, showWarning, showInfo } = useToastNotification();
  const queryClient = useQueryClient();

  const { data: unreadCount = 0 } = useUnreadCounter();

  const allQuery = useNotificationsInfinite('all', isOpen);
  const unreadQuery = useNotificationsInfinite('unread', isOpen && activeTab === 1);

  const currentQuery = activeTab === 0 ? allQuery : unreadQuery;
  const allNotifications = allQuery.data?.pages.flat() ?? [];
  const unreadNotifications = unreadQuery.data?.pages.flat() ?? [];
  const loading = currentQuery.isLoading;
  const loadingMore = currentQuery.isFetchingNextPage;
  const hasMore = currentQuery.hasNextPage ?? false;

  const markAsReadMutation = useMarkAsRead();
  const markAllAsReadMutation = useMarkAllAsRead();

  const bgColor = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const hoverBg = useColorModeValue("gray.50", "gray.700");
  const unreadBg = useColorModeValue("primary.50", "primary.900");
  const messageTextColor = useColorModeValue("gray.700", "gray.300");

  const getToastStatus = (type: NotificationType): "success" | "warning" | "error" | "info" => {
    switch (type) {
      case NotificationType.Success: return "success";
      case NotificationType.Warning: return "warning";
      case NotificationType.Error: return "error";
      default: return "info";
    }
  };

  const showNotificationToast = (
    title: string,
    description: string,
    type: NotificationType
  ) => {
    const toastStatus = getToastStatus(type);
    const options = { duration: 5000 };

    switch (toastStatus) {
      case "success":
        showSuccess(title, description, options);
        break;
      case "warning":
        showWarning(title, description, options);
        break;
      case "error":
        showError(title, description, options);
        break;
      default:
        showInfo(title, description, options);
        break;
    }
  };

  // SignalR - nasłuchuj na nowe powiadomienia
  useEffect(() => {
    const unsubscribeNew = notificationHubService.onNotificationReceived((payload) => {
      // Aktualizuj licznik bezpośrednio ze snapshotu
      queryClient.setQueryData(
        notificationKeys.unreadCounter(),
        payload.unreadNotificationCounter
      );
      // Invaliduj listy żeby nowe powiadomienie pojawiło się
      queryClient.invalidateQueries({ queryKey: notificationKeys.all });

      const metadata = payload.notification.metadata ?? undefined;
      if (isAICostImportNotification(metadata)) {
        queryClient.invalidateQueries({ queryKey: aiCostImportKeys.all });
      }

      showNotificationToast(
        payload.notification.title,
        payload.notification.message,
        payload.notification.type
      );
    });

    const unsubscribeSync = notificationHubService.onNotificationSynced(() => {
      queryClient.invalidateQueries({ queryKey: notificationKeys.all });
    });

    return () => {
      unsubscribeNew();
      unsubscribeSync();
    };
  }, [queryClient, showSuccess, showWarning, showError, showInfo]);

  const handleNotificationClick = (notification: NotificationWeb) => {
    const route = resolveNotificationRoute(notification.metadata ?? undefined);

    if (!notification.isRead) {
      markAsReadMutation.mutate(notification.id);
    }

    if (route) {
      setIsOpen(false);
      navigate(route);
    }
  };

  const handleMarkAllAsRead = () => {
    markAllAsReadMutation.mutate(undefined, {
      onSuccess: (result) => {
        if (result.markedCount > 0) {
          showSuccess(
            "Oznaczono wszystkie jako przeczytane",
            `Oznaczono ${result.markedCount} powiadomień`
          );
        }
      },
    });
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

  const formatDate = (dateString: string) => getRelativeTime(dateString);

  const renderNotifications = (notifications: NotificationWeb[]) => (
    loading ? (
      <Box textAlign="center" py={8}>
        <Spinner size="lg" color="primary.500" />
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
            bg={!notification.isRead ? unreadBg : "transparent"}
            borderBottom="1px"
            borderColor={borderColor}
            _hover={{ bg: hoverBg }}
            transition="background 0.2s"
            cursor="pointer"
            onClick={() => handleNotificationClick(notification)}
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
                  {!notification.isRead && (
                    <Badge colorScheme="primary" fontSize="xs" flexShrink={0}>
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
        
        {/* Przycisk "Załaduj więcej" */}
        {!loading && notifications.length > 0 && hasMore && (
          <Box p={3} borderTop="1px" borderColor={borderColor}>
            <Button
              size="sm"
              variant="ghost"
              colorScheme="primary"
              onClick={() => currentQuery.fetchNextPage()}
              isLoading={loadingMore}
              w="full"
            >
              Załaduj więcej
            </Button>
          </Box>
        )}
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
          <VStack align="stretch" spacing={2}>
            <HStack justify="space-between">
              <Text>Powiadomienia</Text>
              {unreadCount > 0 && (
                <Badge colorScheme="primary">{unreadCount} nowych</Badge>
              )}
            </HStack>
            {unreadCount > 0 && (
              <Button
                size="xs"
                variant="ghost"
                colorScheme="primary"
                onClick={handleMarkAllAsRead}
                leftIcon={<Icon as={CheckCircle} boxSize={3} />}
                justifyContent="flex-start"
              >
                Oznacz wszystkie jako przeczytane
              </Button>
            )}
          </VStack>
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
