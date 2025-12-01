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

  // Pobierz powiadomienia przy otwarciu
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

  // Polling co 30 sekund dla licznika nieprzeczytanych
  useEffect(() => {
    fetchNotifications();
    const interval = setInterval(fetchNotifications, 30000);
    return () => clearInterval(interval);
  }, []);

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
                >
                  <HStack align="flex-start" spacing={3}>
                    <Icon
                      as={getNotificationIcon(notification.type)}
                      boxSize={5}
                      color={`${getNotificationColor(notification.type)}.500`}
                      mt={0.5}
                    />
                    <VStack align="flex-start" spacing={1} flex={1}>
                      <HStack justify="space-between" w="full">
                        <Text fontWeight="semibold" fontSize="sm" noOfLines={2}>
                          {notification.title}
                        </Text>
                        {!notification.readed && (
                          <Badge colorScheme="blue" fontSize="xs">
                            Nowe
                          </Badge>
                        )}
                      </HStack>
                      <Text fontSize="xs" color="gray.600" noOfLines={2}>
                        {notification.message}
                      </Text>
                      <HStack justify="space-between" w="full" mt={1}>
                        <VStack align="flex-start" spacing={0}>
                          <Text fontSize="xs" color="gray.500">
                            {formatDate(notification.createdAt)}
                          </Text>
                          {notification.projectName && (
                            <Text fontSize="xs" color="gray.500" fontWeight="medium">
                              Projekt: {notification.projectName}
                            </Text>
                          )}
                        </VStack>
                      </HStack>
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
