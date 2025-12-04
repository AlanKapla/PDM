import {
  Box,
  VStack,
  Button,
  Collapse,
  Badge,
  Text,
} from "@chakra-ui/react";
import {
  Building2,
  ChevronDown,
  ChevronUp,
  FolderKanban,
  FileText,
  Calculator,
} from "lucide-react";

import { useNavigate, useLocation } from "react-router-dom";
import { useState, useEffect } from "react";
import { getActiveInvitations } from "../services/tenantService";
import { InvitationStatus } from "../types/auth.types";

export default function Sidebar() {
  const navigate = useNavigate();
  const location = useLocation();

  const [invitations, setInvitations] = useState(0);
  const [expanded, setExpanded] = useState(() => {
    return localStorage.getItem("sidebar_expand") === "true";
  });

  const bg = "#696969ff";
  const hoverBg = "rgba(255,255,255,0.06)";
  const activeBg = "linear-gradient(90deg, rgba(90,113,255,0.20), rgba(90,113,255,0.08))";

  const text = "rgba(255,255,255,0.85)";
  const textMuted = "rgba(255,255,255,0.5)";

  useEffect(() => {
    getActiveInvitations()
      .then((inv) => {
        const pending = inv.filter((i: any) => i.status === InvitationStatus.Pending);
        setInvitations(pending.length);
      })
      .catch(() => {});
  }, []);

  useEffect(() => {
    if (location.pathname.startsWith("/tenants")) {
      setExpanded(true);
    }
  }, [location.pathname]);

  useEffect(() => {
    localStorage.setItem("sidebar_expand", String(expanded));
  }, [expanded]);

  const buttonStyle = {
    w: "100%",
    justifyContent: "flex-start",
    color: text,
    bg: "transparent",
    fontWeight: "500",
    transition: "0.15s ease",
    _hover: { bg: hoverBg, transform: "translateX(2px)" },
  };

  return (
    <Box
      position="fixed"
      left="0"
      top="64px"
      w="260px"
      h="calc(100vh - 64px)"
      bg={bg}
      borderRight="1px solid rgba(255,255,255,0.1)"
      px={6}
      py={7}
    >
      <VStack align="flex-start" spacing={3} w="100%">
        <Text fontSize="xs" color={textMuted} letterSpacing="0.1em">
          Nawigacja
        </Text>

        {/* ORGANIZACJE */}
        <Button
          {...buttonStyle}
          leftIcon={<Building2 size={18} />}
          rightIcon={expanded ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
          bg={location.pathname.startsWith("/tenants") ? activeBg : "transparent"}
          onClick={() => setExpanded(!expanded)}
        >
          Organizacje
        </Button>

        <Collapse in={expanded}>
          <VStack align="stretch" spacing={2} pl={4}>
            <Button
              {...buttonStyle}
              fontSize="sm"
              bg={location.pathname === "/tenants/invitations" ? activeBg : "transparent"}
              rightIcon={
                invitations > 0 ? (
                  <Badge colorScheme="red" borderRadius="full" px={2}>
                    {invitations}
                  </Badge>
                ) : undefined
              }
              onClick={() => navigate("/tenants/invitations")}
            >
              Aktywne zaproszenia
            </Button>

            <Button
              {...buttonStyle}
              fontSize="sm"
              bg={location.pathname === "/tenants/collaborating" ? activeBg : "transparent"}
              onClick={() => navigate("/tenants/collaborating")}
            >
              Z którymi współpracujesz
            </Button>

            <Button
              {...buttonStyle}
              fontSize="sm"
              bg={location.pathname === "/tenants/managed" ? activeBg : "transparent"}
              onClick={() => navigate("/tenants/managed")}
            >
              Którymi zarządzasz
            </Button>
          </VStack>
        </Collapse>

        {/* STATYCZNE SEKCJE */}
        <Button
          {...buttonStyle}
          leftIcon={<FolderKanban size={18} />}
          bg={location.pathname.startsWith("/projects") ? activeBg : "transparent"}
          onClick={() => navigate("/projects")}
        >
          Projekty
        </Button>

        <Button
          {...buttonStyle}
          leftIcon={<FileText size={18} />}
          bg={location.pathname.startsWith("/files") ? activeBg : "transparent"}
          onClick={() => navigate("/files")}
        >
          Pliki
        </Button>

        <Button
          {...buttonStyle}
          leftIcon={<Calculator size={18} />}
          bg={location.pathname.startsWith("/cost-editor") ? activeBg : "transparent"}
          onClick={() => navigate("/cost-editor")}
        >
          Kosztorysy
        </Button>
      </VStack>
    </Box>
  );
}
