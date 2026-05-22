import { useContext } from "react";
import {
  Box,
  HStack,
  VStack,
  Text,
  Badge,
  useColorModeValue,
} from "@chakra-ui/react";
import {
  FolderKanban,
  MessageSquare,
  CalendarClock,
  Mail,
  Settings,
} from "lucide-react";
import { useLocation, useNavigate } from "react-router-dom";
import { ChatUnreadContext } from "../../context/ChatUnreadContext";

interface NavItem {
  label: string;
  icon: React.ElementType;
  to: string;
  matchPrefix?: boolean;
  badge?: number;
}

/**
 * Dolny pasek nawigacji dla urządzeń mobilnych (base/sm).
 * Na desktop ukryty — nawigacja odbywa się przez sidebar.
 */
export default function BottomNavBar({
  invitationsCount = 0,
}: {
  invitationsCount?: number;
}) {
  const location = useLocation();
  const navigate = useNavigate();
  const { totalUnread } = useContext(ChatUnreadContext);

  const bg = useColorModeValue("white", "gray.900");
  const border = useColorModeValue("gray.200", "gray.700");
  const activeColor = "primary.600";
  const inactiveColor = useColorModeValue("gray.500", "gray.400");

  const navItems: NavItem[] = [
    {
      label: "Projekty",
      icon: FolderKanban,
      to: "/projects",
    },
    {
      label: "Zaproszenia",
      icon: Mail,
      to: "/tenants/invitations",
      badge: invitationsCount > 0 ? invitationsCount : undefined,
    },
    {
      label: "Wiadomości",
      icon: MessageSquare,
      to: "/chat",
      matchPrefix: true,
      badge: totalUnread > 0 ? totalUnread : undefined,
    },
    {
      label: "Prace",
      icon: CalendarClock,
      to: "/assigned-works",
    },
    {
      label: "Ustawienia",
      icon: Settings,
      to: "/profile",
    },
  ];

  const isActive = (item: NavItem) =>
    item.matchPrefix
      ? location.pathname.startsWith(item.to)
      : location.pathname === item.to;

  return (
    <Box
      display={{ base: "flex", md: "none" }}
      position="fixed"
      bottom={0}
      left={0}
      right={0}
      zIndex={900}
      bg={bg}
      borderTop="1px solid"
      borderColor={border}
      pb={2}
    >
      <HStack w="full" justify="space-around" px={1} py={1}>
        {navItems.map((item) => {
          const active = isActive(item);
          return (
            <VStack
              key={item.to}
              as="button"
              type="button"
              spacing={0.5}
              flex={1}
              cursor="pointer"
              onClick={() => navigate(item.to)}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  e.preventDefault();
                  navigate(item.to);
                }
              }}
              py={1}
              borderRadius="md"
              color={active ? activeColor : inactiveColor}
              _active={{ bg: useColorModeValue("gray.100", "gray.700") }}
              position="relative"
              aria-label={item.label}
            >
              <Box position="relative">
                <Box
                  display="flex"
                  alignItems="center"
                  justifyContent="center"
                  w="32px"
                  h="32px"
                  color={active ? activeColor : inactiveColor}
                  pointerEvents="none"
                >
                  <item.icon size={22} />
                </Box>
                {item.badge !== undefined && (
                  <Badge
                    colorScheme={item.to === "/chat" ? "primary" : "red"}
                    borderRadius="full"
                    fontSize="2xs"
                    minW="16px"
                    h="16px"
                    display="flex"
                    alignItems="center"
                    justifyContent="center"
                    position="absolute"
                    top="2px"
                    right="2px"
                  >
                    {item.badge > 99 ? "99+" : item.badge}
                  </Badge>
                )}
              </Box>
              <Text
                fontSize="2xs"
                fontWeight={active ? "semibold" : "normal"}
                textAlign="center"
                lineHeight="1"
              >
                {item.label}
              </Text>
            </VStack>
          );
        })}
      </HStack>
    </Box>
  );
}
