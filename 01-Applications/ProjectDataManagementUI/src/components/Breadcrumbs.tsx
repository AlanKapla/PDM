import { Breadcrumb, BreadcrumbItem, BreadcrumbLink, Box, useColorModeValue } from "@chakra-ui/react";
import { ChevronRight } from "lucide-react";
import { Link, useLocation, useParams } from "react-router-dom";
import { useEffect, useState } from "react";
import { projectApi } from "../api/projectApi";
import { costEstimateTemplateApi } from "../api/costEstimateTemplateApi";
import { costEstimateApi } from "../api/costEstimateApi";
import { useAuth } from "../context/AuthContext";
import { useGlobalCache } from "../hooks/useGlobalCache";

interface BreadcrumbSegment {
  label: string;
  path: string;
  isCurrentPage?: boolean;
}

export default function Breadcrumbs() {
  const location = useLocation();
  const params = useParams();
  const { user } = useAuth();
  const [breadcrumbs, setBreadcrumbs] = useState<BreadcrumbSegment[]>([]);
  const [projectName, setProjectName] = useState<string>("");
  const [templateName, setTemplateName] = useState<string>("");
  const [costEstimateName, setCostEstimateName] = useState<string>("");

  const borderColor = useColorModeValue("gray.200", "gray.700");
  const bgColor = useColorModeValue("white", "gray.800");

  // Globalny cache dla project details (współdzielony z innymi komponentami)
  const projectDetailsCache = useGlobalCache(
    `project-details-${params.projectId}`,
    async () => {
      if (!user?.activeTenantId || !params.projectId) throw new Error('Missing tenant or project ID');
      const res = await projectApi.getProjectDetails(user.activeTenantId, params.projectId);
      return res.data;
    }
  );

  // Globalny cache dla template details
  const templateDetailsCache = useGlobalCache(
    `template-details-${params.templateId}`,
    async () => {
      if (!params.templateId) throw new Error('Missing template ID');
      return await costEstimateTemplateApi.getTemplateDetails(params.templateId);
    }
  );

  // Globalny cache dla cost estimate details
  const costEstimateDetailsCache = useGlobalCache(
    `cost-estimate-details-${params.estimateId}`,
    async () => {
      if (!user?.activeTenantId || !params.projectId || !params.estimateId) {
        throw new Error('Missing tenant, project or estimate ID');
      }
      return await costEstimateApi.getCostEstimateDetails(
        user.activeTenantId,
        params.projectId,
        params.estimateId
      );
    }
  );

  // Pobierz nazwę projektu jeśli jest w URL
  // useGlobalCache zapobiega duplikacji requestów - jeśli cache istnieje, zwraca z cache
  useEffect(() => {
    const fetchProjectName = async () => {
      if (params.projectId && user?.activeTenantId) {
        try {
          const projectDetails = await projectDetailsCache.fetch();
          setProjectName(projectDetails.name);
        } catch (error) {
          setProjectName("");
        }
      } else {
        setProjectName("");
      }
    };

    fetchProjectName();
  }, [params.projectId, user?.activeTenantId]);

  // Pobierz nazwę szablonu jeśli jest w URL
  useEffect(() => {
    const fetchTemplateName = async () => {
      if (params.templateId) {
        try {
          const templateDetails = await templateDetailsCache.fetch();
          setTemplateName(templateDetails.name);
        } catch (error) {
          setTemplateName("");
        }
      } else {
        setTemplateName("");
      }
    };

    fetchTemplateName();
  }, [params.templateId]);

  // Pobierz nazwę kosztorysu jeśli jest w URL
  useEffect(() => {
    const fetchCostEstimateName = async () => {
      if (params.estimateId && params.projectId && user?.activeTenantId) {
        try {
          const costEstimateDetails = await costEstimateDetailsCache.fetch();
          setCostEstimateName(costEstimateDetails.name);
        } catch (error) {
          setCostEstimateName("");
        }
      } else {
        setCostEstimateName("");
      }
    };

    fetchCostEstimateName();
  }, [params.estimateId, params.projectId, user?.activeTenantId]);

  // Generuj breadcrumbs po załadowaniu nazwy projektu
  useEffect(() => {
    const generateBreadcrumbs = () => {
      const pathSegments = location.pathname.split("/").filter(Boolean);
      const segments: BreadcrumbSegment[] = [
        { label: "Panel główny", path: "/dashboard" }
      ];

      // Mapowanie ścieżek
      if (pathSegments[0] === "tenants") {
        if (pathSegments[1] === "invitations") {
          segments.push({ label: "Zaproszenia", path: "/tenants/invitations", isCurrentPage: true });
        } else if (pathSegments[1] === "collaborating") {
          segments.push({ label: "Przełącz organizację", path: "/tenants/collaborating", isCurrentPage: true });
        } else if (pathSegments[1] === "managed") {
          segments.push({ label: "Zarządzaj", path: "/tenants/managed", isCurrentPage: true });
        } else if (params.tenantId) {
          segments.push({ label: "Szczegóły organizacji", path: `/tenants/${params.tenantId}`, isCurrentPage: true });
        }
      } else if (pathSegments[0] === "projects") {
        segments.push({ label: "Projekty", path: "/projects" });
        
        if (params.projectId) {
          const projectLabel = projectName || "Projekt";
          segments.push({ label: projectLabel, path: `/projects/${params.projectId}` });
          
          if (pathSegments[2] === "members") {
            segments.push({ label: "Członkowie", path: `/projects/${params.projectId}/members`, isCurrentPage: true });
          } else if (pathSegments[2] === "schedules") {
            if (params.workScheduleId) {
              segments.push({ label: "Harmonogramy", path: `/projects/${params.projectId}/schedules` });
              segments.push({ label: "Harmonogram", path: location.pathname, isCurrentPage: true });
            } else {
              segments.push({ label: "Harmonogramy", path: `/projects/${params.projectId}/schedules`, isCurrentPage: true });
            }
          } else if (pathSegments[2] === "files") {
            segments.push({ label: "Pliki", path: `/projects/${params.projectId}/files`, isCurrentPage: true });
          } else if (pathSegments[2] === "costs") {
            segments.push({ label: "Wydatki", path: `/projects/${params.projectId}/costs`, isCurrentPage: true });
          } else if (pathSegments[2] === "cost-estimates") {
            if (params.estimateId) {
              segments.push({ label: "Kosztorysy", path: `/projects/${params.projectId}/cost-estimates` });
              const costEstimateLabel = costEstimateName || "Kosztorys";
              segments.push({ label: costEstimateLabel, path: location.pathname, isCurrentPage: true });
            } else {
              segments.push({ label: "Kosztorysy", path: `/projects/${params.projectId}/cost-estimates`, isCurrentPage: true });
            }
          } else {
            segments[segments.length - 1].isCurrentPage = true;
          }
        } else {
          segments[segments.length - 1].isCurrentPage = true;
        }
      } else if (pathSegments[0] === "assigned-works") {
        segments.push({ label: "Zaplanowane prace", path: "/assigned-works", isCurrentPage: true });
      } else if (pathSegments[0] === "cost-estimate-templates") {
        segments.push({ label: "Szablony kosztorysów", path: "/cost-estimate-templates" });
        
        if (params.templateId) {
          const templateLabel = templateName || "Szablon";
          
          if (pathSegments[2] === "versions") {
            segments.push({ label: templateLabel, path: `/cost-estimate-templates/${params.templateId}/edit` });
            segments.push({ label: "Historia wersji", path: `/cost-estimate-templates/${params.templateId}/versions`, isCurrentPage: true });
          } else if (pathSegments[2] === "edit") {
            segments.push({ label: templateLabel, path: `/cost-estimate-templates/${params.templateId}/edit`, isCurrentPage: true });
          } else {
            segments[segments.length - 1].isCurrentPage = true;
          }
        } else if (pathSegments[1] === "new") {
          segments.push({ label: "Nowy szablon", path: "/cost-estimate-templates/new", isCurrentPage: true });
        } else if (pathSegments[1] === "select") {
          segments.push({ label: "Wybierz szablon", path: "/cost-estimate-templates/select", isCurrentPage: true });
        } else {
          segments[segments.length - 1].isCurrentPage = true;
        }
      } else if (pathSegments[0] === "profile") {
        segments.push({ label: "Ustawienia", path: "/profile", isCurrentPage: true });
      } else if (pathSegments[0] === "dashboard") {
        segments[0].isCurrentPage = true;
      }

      setBreadcrumbs(segments);
    };

    generateBreadcrumbs();
  }, [location.pathname, params.projectId, params.tenantId, params.estimateId, params.workScheduleId, params.templateId, projectName, templateName, costEstimateName]);

  if (breadcrumbs.length <= 1) {
    return null;
  }

  // Nie renderuj breadcrumbs dopóki nazwa projektu/szablonu się nie załaduje
  if (params.projectId && !projectName && projectDetailsCache.loading) {
    return null;
  }
  
  if (params.templateId && !templateName && templateDetailsCache.loading) {
    return null;
  }

  return (
    <Box 
      px={{ base: 3, sm: 4, md: 10 }} 
      py={{ base: 2, md: 3 }}
      borderBottom="1px solid" 
      borderColor={borderColor}
      bg={bgColor}
    >
      <Breadcrumb spacing={1} separator={<ChevronRight size={14} />} fontSize={{ base: "xs", md: "sm" }}>
        {breadcrumbs.map((crumb, index) => (
          <BreadcrumbItem key={index} isCurrentPage={crumb.isCurrentPage}>
            {crumb.isCurrentPage ? (
              <BreadcrumbLink fontWeight="semibold" color="blue.600" noOfLines={1}>
                {crumb.label}
              </BreadcrumbLink>
            ) : (
              <BreadcrumbLink as={Link} to={crumb.path} noOfLines={1}>
                {crumb.label}
              </BreadcrumbLink>
            )}
          </BreadcrumbItem>
        ))}
      </Breadcrumb>
    </Box>
  );
}
