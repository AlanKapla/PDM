import { useEffect, useMemo, useState } from "react";
import {
  Box,
  Heading,
  Text,
  HStack,
  VStack,
  Button,
  Input,
  InputGroup,
  InputLeftElement,
  Badge,
  Avatar,
  AvatarGroup,
  Divider,
  useColorModeValue,
  useToast,
  useDisclosure,
  Drawer,
  DrawerOverlay,
  DrawerContent,
  DrawerHeader,
  DrawerCloseButton,
  DrawerBody,
  DrawerFooter,
  FormControl,
  FormLabel,
  SimpleGrid,
  Select,
  Tag,
  TagLabel,
  TagLeftIcon,
  Skeleton,
  SkeletonText,
  Tabs,
  TabList,
  Tab,
  Tooltip,
} from "@chakra-ui/react";
import { motion } from "framer-motion";
import {
  Plus,
  Edit2,
  UserPlus,
  CheckCircle2,
  Users,
  Building2,
  ArrowRight,
  Search,
  Filter,
  ArrowUpAz,
  ArrowDownAz,
  Mail,
} from "lucide-react";

import MainLayout from "../layout/MainLayout";

import {
  getUserTenants,
  getActiveTenant,
  changeActiveTenant,
  createTenant,
  updateTenant,
  inviteTenantMember,
} from "../services/tenantService";

import {
  TenantRole,
  getTenantRoleName,
  getTenantRoleColor,
} from "../types/auth.types";
import type { TenantDetails } from "../types/auth.types";

const MotionBox = motion(Box);

type TenantScopeTab = "all" | "managed" | "collaborating";
type SortOption = "nameAsc" | "nameDesc" | "createdDesc" | "membersDesc";

export default function Tenants() {
  const [tenants, setTenants] = useState<TenantDetails[]>([]);
  const [activeTenantId, setActiveTenantId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const [search, setSearch] = useState("");
  const [scope, setScope] = useState<TenantScopeTab>("all");
  const [roleFilter, setRoleFilter] = useState<"all" | "admin" | "member">(
    "all"
  );
  const [sort, setSort] = useState<SortOption>("createdDesc");

  const [selectedTenant, setSelectedTenant] = useState<TenantDetails | null>(
    null
  );
  const [inviteEmail, setInviteEmail] = useState("");
  const [editName, setEditName] = useState("");
  const [newTenantName, setNewTenantName] = useState("");

  const [inviteLoading, setInviteLoading] = useState(false);
  const [editLoading, setEditLoading] = useState(false);
  const [createLoading, setCreateLoading] = useState(false);

  const toast = useToast();

  const {
    isOpen: isInviteOpen,
    onOpen: openInvite,
    onClose: closeInvite,
  } = useDisclosure();
  const {
    isOpen: isEditOpen,
    onOpen: openEdit,
    onClose: closeEdit,
  } = useDisclosure();
  const {
    isOpen: isCreateOpen,
    onOpen: openCreate,
    onClose: closeCreate,
  } = useDisclosure();

  const cardBg = useColorModeValue("#101010", "#101010");
  const border = useColorModeValue("#1e1e1e", "#1e1e1e");
  const activeBg = useColorModeValue("#0b1220", "#0b1220");
  const muted = useColorModeValue("gray.500", "gray.400");
  const subtle = useColorModeValue("#111827", "#111827");

  // --------------------------------------------------
  // LOAD DATA
  // --------------------------------------------------

  useEffect(() => {
    async function load() {
      setLoading(true);
      try {
        const [tenantList, active] = await Promise.all([
          getUserTenants(),
          getActiveTenant(),
        ]);
        setTenants(tenantList);
        setActiveTenantId(active?.activeTenantId ?? null);
      } catch (err) {
        console.error("Błąd ładowania organizacji", err);
        toast({
          title: "Błąd ładowania organizacji",
          status: "error",
          duration: 4000,
          isClosable: true,
        });
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [toast]);

  const refreshTenants = async () => {
    setRefreshing(true);
    try {
      const tenantList = await getUserTenants();
      setTenants(tenantList);
    } catch {
      // cichy fail + toast
      toast({
        title: "Nie udało się odświeżyć listy",
        status: "error",
        duration: 3000,
        isClosable: true,
      });
    } finally {
      setRefreshing(false);
    }
  };

  // --------------------------------------------------
  // DERIVED DATA
  // --------------------------------------------------

  const managedTenants = useMemo(
    () => tenants.filter((t) => t.role === TenantRole.Admin),
    [tenants]
  );
  const collaboratingTenants = useMemo(
    () => tenants.filter((t) => t.role !== TenantRole.Admin),
    [tenants]
  );

  const stats = useMemo(() => {
    const total = tenants.length;
    const managed = managedTenants.length;
    const collaborating = collaboratingTenants.length;

    const totalMembers = tenants.reduce(
      (acc, t) => acc + (t.members?.length ?? 0),
      0
    );

    const pendingInvitations = tenants.reduce((acc, t) => {
      const pending =
        t.invitations?.filter((i) => i.status === 0 /* Pending */)?.length ??
        0;
      return acc + pending;
    }, 0);

    return { total, managed, collaborating, totalMembers, pendingInvitations };
  }, [tenants, managedTenants, collaboratingTenants]);

  const filteredTenants = useMemo(() => {
    let list = tenants.slice();

    // scope
    if (scope === "managed") {
      list = list.filter((t) => t.role === TenantRole.Admin);
    } else if (scope === "collaborating") {
      list = list.filter((t) => t.role !== TenantRole.Admin);
    }

    // role filter
    if (roleFilter === "admin") {
      list = list.filter((t) => t.role === TenantRole.Admin);
    }
    if (roleFilter === "member") {
      list = list.filter((t) => t.role !== TenantRole.Admin);
    }

    // search
    if (search.trim()) {
      const s = search.toLowerCase();
      list = list.filter((t) => t.name.toLowerCase().includes(s));
    }

    // sort
    list.sort((a, b) => {
      switch (sort) {
        case "nameAsc":
          return a.name.localeCompare(b.name);
        case "nameDesc":
          return b.name.localeCompare(a.name);
        case "membersDesc":
          return (
            (b.members?.length ?? 0) - (a.members?.length ?? 0)
          );
        case "createdDesc":
        default:
          return (
            new Date(b.createdAt).getTime() -
            new Date(a.createdAt).getTime()
          );
      }
    });

    return list;
  }, [tenants, scope, roleFilter, search, sort]);

  const findActiveTenantName = () => {
    const t = tenants.find((x) => x.id === activeTenantId);
    return t?.name ?? "Brak aktywnej organizacji";
  };

  // --------------------------------------------------
  // ACTIONS
  // --------------------------------------------------

  const handleActivate = async (id: string) => {
    if (!id || id === activeTenantId) return;

    try {
      const ok = await changeActiveTenant(id);
      if (ok) {
        setActiveTenantId(id);
        toast({
          title: "Aktywna organizacja zmieniona",
          status: "success",
          duration: 3000,
          isClosable: true,
        });
        setTimeout(() => window.location.reload(), 400);
      } else {
        toast({
          title: "Nie udało się zmienić aktywnej organizacji",
          status: "error",
          duration: 3000,
          isClosable: true,
        });
      }
    } catch {
      toast({
        title: "Błąd zmiany organizacji",
        status: "error",
        duration: 3000,
        isClosable: true,
      });
    }
  };

  const handleInvite = async () => {
    if (!selectedTenant || !inviteEmail.trim()) return;

    const email = inviteEmail.trim();
    setInviteLoading(true);

    try {
      const ok = await inviteTenantMember(selectedTenant.id, email);
      if (ok) {
        toast({
          title: "Zaproszenie wysłane",
          description: `Wysłano zaproszenie do: ${email}`,
          status: "success",
          duration: 4000,
          isClosable: true,
        });
        setInviteEmail("");
        closeInvite();
        refreshTenants();
      } else {
        toast({
          title: "Błąd wysyłania zaproszenia",
          status: "error",
          duration: 3000,
          isClosable: true,
        });
      }
    } catch {
      toast({
        title: "Błąd połączenia",
        status: "error",
        duration: 3000,
        isClosable: true,
      });
    } finally {
      setInviteLoading(false);
    }
  };

  const handleEdit = async () => {
    if (!selectedTenant || !editName.trim()) return;
    const name = editName.trim();

    setEditLoading(true);
    try {
      const updated = await updateTenant(selectedTenant.id, name);
      if (updated) {
        setTenants((prev) =>
          prev.map((t) => (t.id === selectedTenant.id ? updated : t))
        );
        toast({
          title: "Organizacja zaktualizowana",
          status: "success",
          duration: 3000,
          isClosable: true,
        });
        closeEdit();
      } else {
        toast({
          title: "Nie udało się zaktualizować organizacji",
          status: "error",
          duration: 3000,
          isClosable: true,
        });
      }
    } catch {
      toast({
        title: "Błąd połączenia",
        status: "error",
        duration: 3000,
        isClosable: true,
      });
    } finally {
      setEditLoading(false);
    }
  };

  const handleCreate = async () => {
    if (!newTenantName.trim()) return;

    const name = newTenantName.trim();
    setCreateLoading(true);
    try {
      const created = await createTenant(name);
      if (created) {
        setTenants((prev) => [...prev, created]);
        toast({
          title: "Organizacja utworzona",
          status: "success",
          duration: 3000,
          isClosable: true,
        });
        setNewTenantName("");
        closeCreate();
      } else {
        toast({
          title: "Nie udało się utworzyć organizacji",
          status: "error",
          duration: 3000,
          isClosable: true,
        });
      }
    } catch {
      toast({
        title: "Błąd połączenia",
        status: "error",
        duration: 3000,
        isClosable: true,
      });
    } finally {
      setCreateLoading(false);
    }
  };

  // --------------------------------------------------
  // RENDER
  // --------------------------------------------------

  const renderCardSkeleton = () => (
    <SimpleGrid columns={{ base: 1, md: 2 }} spacing={6}>
      {[1, 2, 3, 4].map((i) => (
        <Box
          key={i}
          p={6}
          bg={cardBg}
          rounded="xl"
          border="1px solid"
          borderColor={border}
        >
          <Skeleton height="20px" mb={4} />
          <SkeletonText noOfLines={3} spacing="3" />
        </Box>
      ))}
    </SimpleGrid>
  );

  return (
    <MainLayout>
      <Box maxW="1200px" mx="auto" px={4} py={10}>
        {/* HEADER */}
        <HStack justify="space-between" align="flex-start" mb={8} spacing={6}>
          <VStack align="flex-start" spacing={2}>
            <Heading size="lg">Organizacje</Heading>
            <Text fontSize="sm" color={muted}>
              Zarządzaj przestrzeniami roboczymi, członkami i uprawnieniami.
            </Text>

            <HStack spacing={3} mt={2} flexWrap="wrap">
              <Tag size="sm" variant="subtle" bg={subtle} color="gray.200">
                <TagLeftIcon as={Building2} />
                <TagLabel>{stats.total} organizacji</TagLabel>
              </Tag>
              <Tag size="sm" variant="subtle" bg={subtle} color="gray.200">
                <TagLeftIcon as={Users} />
                <TagLabel>{stats.totalMembers} użytkowników łącznie</TagLabel>
              </Tag>
              {stats.pendingInvitations > 0 && (
                <Tag
                  size="sm"
                  variant="subtle"
                  colorScheme="yellow"
                  bg="yellow.900"
                >
                  <TagLeftIcon as={Mail} />
                  <TagLabel>
                    {stats.pendingInvitations} oczekujących zaproszeń
                  </TagLabel>
                </Tag>
              )}
            </HStack>
          </VStack>

          <VStack align="flex-end" spacing={3}>
            <Button
              leftIcon={<Plus size={18} />}
              colorScheme="blue"
              onClick={openCreate}
              isLoading={createLoading}
            >
              Nowa organizacja
            </Button>

            <Tooltip label={findActiveTenantName()}>
              <Tag
                size="sm"
                variant="subtle"
                bg={activeBg}
                color="blue.100"
                borderRadius="full"
              >
                <TagLeftIcon as={CheckCircle2} />
                <TagLabel maxW="200px" isTruncated>
                  Aktywna: {findActiveTenantName()}
                </TagLabel>
              </Tag>
            </Tooltip>
          </VStack>
        </HStack>

        {/* FILTER BAR */}
        <Box
          mb={6}
          p={3}
          bg="#050505"
          border="1px solid"
          borderColor={border}
          rounded="xl"
        >
          <HStack justify="space-between" align="center" spacing={4}>
            {/* Tabs scope */}
            <Tabs
              variant="unstyled"
              index={["all", "managed", "collaborating"].indexOf(scope)}
              onChange={(i) =>
                setScope(
                  ["all", "managed", "collaborating"][i] as TenantScopeTab
                )
              }
            >
              <TabList>
                <Tab
                  _selected={{ color: "white", bg: "#111827" }}
                  fontSize="sm"
                >
                  Wszystkie
                </Tab>
                <Tab
                  _selected={{ color: "white", bg: "#111827" }}
                  fontSize="sm"
                >
                  Zarządzasz
                </Tab>
                <Tab
                  _selected={{ color: "white", bg: "#111827" }}
                  fontSize="sm"
                >
                  Współpracujesz
                </Tab>
              </TabList>
            </Tabs>

            <HStack spacing={3} flex={1} justify="flex-end">
              <InputGroup maxW="260px">
                <InputLeftElement pointerEvents="none">
                  <Search size={16} opacity={0.7} />
                </InputLeftElement>
                <Input
                  size="sm"
                  placeholder="Szukaj organizacji..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
              </InputGroup>

              <HStack spacing={2}>
                <Select
                  size="sm"
                  maxW="150px"
                  icon={<Filter size={14} />}
                  value={roleFilter}
                  onChange={(e) =>
                    setRoleFilter(e.target.value as "all" | "admin" | "member")
                  }
                >
                  <option value="all">Rola: wszystkie</option>
                  <option value="admin">Tylko administrator</option>
                  <option value="member">Tylko członek</option>
                </Select>

                <Select
                  size="sm"
                  maxW="170px"
                  icon={
                    sort === "nameAsc" || sort === "nameDesc" ? (
                      <ArrowUpAz size={14} />
                    ) : (
                      <ArrowDownAz size={14} />
                    )
                  }
                  value={sort}
                  onChange={(e) => setSort(e.target.value as SortOption)}
                >
                  <option value="createdDesc">Najnowsze najpierw</option>
                  <option value="nameAsc">Nazwa A → Z</option>
                  <option value="nameDesc">Nazwa Z → A</option>
                  <option value="membersDesc">Najwięcej członków</option>
                </Select>
              </HStack>
            </HStack>
          </HStack>
        </Box>

        {/* LISTA ORGANIZACJI */}
        {loading ? (
          renderCardSkeleton()
        ) : filteredTenants.length === 0 ? (
          <Box
            mt={10}
            p={10}
            textAlign="center"
            bg={cardBg}
            border="1px dashed"
            borderColor={border}
            rounded="2xl"
          >
            <Heading size="md" mb={3}>
              Brak organizacji
            </Heading>
            <Text color={muted} mb={6}>
              Utwórz pierwszą organizację lub zmień filtry, aby zobaczyć więcej.
            </Text>
            <Button
              leftIcon={<Plus size={18} />}
              colorScheme="blue"
              onClick={openCreate}
            >
              Utwórz organizację
            </Button>
          </Box>
        ) : (
          <SimpleGrid columns={{ base: 1, md: 2 }} spacing={6}>
            {filteredTenants.map((tenant) => {
              const membersCount = tenant.members?.length ?? 0;
              const pendingInvitations =
                tenant.invitations?.filter((i) => i.status === 0).length ?? 0;

              return (
                <MotionBox
                  key={tenant.id}
                  p={6}
                  bg={cardBg}
                  rounded="xl"
                  border="1px solid"
                  borderColor={
                    tenant.id === activeTenantId ? "blue.500" : border
                  }
                  shadow="sm"
                  whileHover={{ scale: 1.015, translateY: -2 }}
                  transition={{ duration: 0.15 }}
                >
                  <HStack justify="space-between" mb={4} align="flex-start">
                    <HStack spacing={3}>
                      <Box
                        w="36px"
                        h="36px"
                        rounded="lg"
                        bg="#020617"
                        display="flex"
                        alignItems="center"
                        justifyContent="center"
                      >
                        <Building2 size={20} />
                      </Box>
                      <VStack align="flex-start" spacing={1}>
                        <Text fontWeight="semibold" fontSize="md">
                          {tenant.name}
                        </Text>
                        <HStack spacing={2}>
                          <Badge
                            colorScheme={getTenantRoleColor(tenant.role)}
                            fontSize="xs"
                          >
                            {getTenantRoleName(tenant.role)}
                          </Badge>
                          <Text fontSize="xs" color={muted}>
                            Utworzono:{" "}
                            {new Date(
                              tenant.createdAt
                            ).toLocaleDateString("pl-PL")}
                          </Text>
                        </HStack>
                      </VStack>
                    </HStack>

                    <VStack align="flex-end" spacing={1}>
                      {tenant.id === activeTenantId && (
                        <Badge
                          colorScheme="blue"
                          display="flex"
                          alignItems="center"
                          gap={1}
                        >
                          <CheckCircle2 size={14} />
                          Aktywna
                        </Badge>
                      )}
                      {pendingInvitations > 0 && (
                        <Badge colorScheme="yellow" fontSize="xs">
                          {pendingInvitations} oczek. zaproszeń
                        </Badge>
                      )}
                    </VStack>
                  </HStack>

                  <VStack align="stretch" spacing={4}>
                    {/* MEMBERS */}
                    <HStack justify="space-between">
                      <VStack align="flex-start" spacing={0}>
                        <Text fontSize="xs" color={muted}>
                          Członkowie
                        </Text>
                        <HStack spacing={2}>
                          <Users size={16} />
                          <Text fontSize="sm" fontWeight="medium">
                            {membersCount}
                          </Text>
                        </HStack>
                      </VStack>

                      <AvatarGroup size="sm" max={4}>
                        {tenant.members?.map((m) => (
                          <Avatar
                            key={m.userId}
                            name={`${m.firstName} ${m.lastName}`}
                          />
                        ))}
                      </AvatarGroup>
                    </HStack>

                    <Divider borderColor={border} />

                    {/* ACTIONS */}
                    <HStack justify="space-between" align="center">
                      <HStack spacing={2}>
                        <Button
                          size="sm"
                          variant="ghost"
                          leftIcon={<UserPlus size={16} />}
                          onClick={() => {
                            setSelectedTenant(tenant);
                            setInviteEmail("");
                            openInvite();
                          }}
                        >
                          Zaproś
                        </Button>
                        <Button
                          size="sm"
                          variant="ghost"
                          leftIcon={<Edit2 size={16} />}
                          onClick={() => {
                            setSelectedTenant(tenant);
                            setEditName(tenant.name);
                            openEdit();
                          }}
                        >
                          Edytuj
                        </Button>
                      </HStack>

                      {tenant.id !== activeTenantId && (
                        <Button
                          size="sm"
                          rightIcon={<ArrowRight size={16} />}
                          colorScheme="blue"
                          variant="outline"
                          onClick={() => handleActivate(tenant.id)}
                          isDisabled={refreshing}
                        >
                          Ustaw jako aktywną
                        </Button>
                      )}
                    </HStack>
                  </VStack>
                </MotionBox>
              );
            })}
          </SimpleGrid>
        )}
      </Box>

      {/* DRAWER: INVITE MEMBER */}
      <Drawer isOpen={isInviteOpen} placement="right" onClose={closeInvite} size="sm">
        <DrawerOverlay />
        <DrawerContent>
          <DrawerCloseButton />
          <DrawerHeader>Zaproś członka</DrawerHeader>
          <DrawerBody>
            <VStack align="stretch" spacing={4}>
              <Text fontSize="sm" color={muted}>
                Organizacja:{" "}
                <strong>{selectedTenant?.name ?? "(brak wybranej)"}</strong>
              </Text>
              <FormControl>
                <FormLabel>Adres email</FormLabel>
                <Input
                  placeholder="jan.kowalski@example.com"
                  value={inviteEmail}
                  onChange={(e) => setInviteEmail(e.target.value)}
                />
              </FormControl>
            </VStack>
          </DrawerBody>
          <DrawerFooter>
            <Button variant="ghost" mr={3} onClick={closeInvite}>
              Anuluj
            </Button>
            <Button
              colorScheme="blue"
              onClick={handleInvite}
              isLoading={inviteLoading}
            >
              Wyślij zaproszenie
            </Button>
          </DrawerFooter>
        </DrawerContent>
      </Drawer>

      {/* DRAWER: EDIT TENANT */}
      <Drawer isOpen={isEditOpen} placement="right" onClose={closeEdit} size="sm">
        <DrawerOverlay />
        <DrawerContent>
          <DrawerCloseButton />
          <DrawerHeader>Edytuj organizację</DrawerHeader>
          <DrawerBody>
            <FormControl>
              <FormLabel>Nazwa organizacji</FormLabel>
              <Input
                value={editName}
                onChange={(e) => setEditName(e.target.value)}
              />
            </FormControl>
          </DrawerBody>
          <DrawerFooter>
            <Button variant="ghost" mr={3} onClick={closeEdit}>
              Anuluj
            </Button>
            <Button
              colorScheme="blue"
              onClick={handleEdit}
              isLoading={editLoading}
            >
              Zapisz zmiany
            </Button>
          </DrawerFooter>
        </DrawerContent>
      </Drawer>

      {/* DRAWER: CREATE TENANT */}
      <Drawer isOpen={isCreateOpen} placement="right" onClose={closeCreate} size="sm">
        <DrawerOverlay />
        <DrawerContent>
          <DrawerCloseButton />
          <DrawerHeader>Nowa organizacja</DrawerHeader>
          <DrawerBody>
            <FormControl>
              <FormLabel>Nazwa organizacji</FormLabel>
              <Input
                placeholder="np. Acme Sp. z o.o."
                value={newTenantName}
                onChange={(e) => setNewTenantName(e.target.value)}
              />
            </FormControl>
          </DrawerBody>
          <DrawerFooter>
            <Button variant="ghost" mr={3} onClick={closeCreate}>
              Anuluj
            </Button>
            <Button
              colorScheme="blue"
              onClick={handleCreate}
              isLoading={createLoading}
            >
              Utwórz
            </Button>
          </DrawerFooter>
        </DrawerContent>
      </Drawer>
    </MainLayout>
  );
}
