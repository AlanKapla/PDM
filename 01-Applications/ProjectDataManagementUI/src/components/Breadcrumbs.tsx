import { Breadcrumb, BreadcrumbItem, BreadcrumbLink, Box, useColorModeValue } from "@chakra-ui/react";
import { ChevronRight } from "lucide-react";
import { Link, useLocation, useParams } from "react-router-dom";
import { useEffect, useState } from "react";
import { projectApi } from "../api/projectApi";
import { tenantApi } from "../api/tenantApi";

interface BreadcrumbSegment {
  label: string;
  path: string;
  isCurrentPage?: boolean;
}

export default function Breadcrumbs() {
  const location = useLocation();
  const params = useParams();
  const [breadcrumbs, setBreadcrumbs] = useState<BreadcrumbSegment[]>([]);
  const [projectName, setProjectName] = useState<string>("");

  const borderColor = useColorModeValue("gray.200", "gray.700");
  const bgColor = useColorModeValue("white", "gray.800");

  useEffect(() => {
    const generateBreadcrumbs = async () => {
      const pathSegments = location.pathname.split("/").filter(Boolean);
      const segments: BreadcrumbSegment[] = [
        { label: "Panel główny", path: "/dashboard" }
      ];

      // Pobierz nazwę projektu jeśli jest w URL
      if (params.projectId) {
        try {
          const activeTenant = await tenantApi.getActiveTenant();
          if (activeTenant.data.activeTenantId) {
            const projectDetails = await projectApi.getProjectDetails(
              activeTenant.data.activeTenantId,
              params.projectId
            );
            setProjectName(projectDetails.data.name);
          }
        } catch (error) {
          console.error("Błąd pobierania nazwy projektu:", error);
        }
      }

      // Mapowanie ścieżek
      if (pathSegments[0] === "tenants") {
        segments.push({ label: "Organizacje", path: "/tenants" });
        
        if (pathSegments[1] === "invitations") {
          segments.push({ label: "Aktywne zaproszenia", path: "/tenants/invitations", isCurrentPage: true });
        } else if (pathSegments[1] === "collaborating") {
          segments.push({ label: "Z którymi współpracujesz", path: "/tenants/collaborating", isCurrentPage: true });
        } else if (pathSegments[1] === "managed") {
          segments.push({ label: "Którymi zarządzasz", path: "/tenants/managed", isCurrentPage: true });
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
            segments.push({ label: "Koszty", path: `/projects/${params.projectId}/costs`, isCurrentPage: true });
          } else if (pathSegments[2] === "cost-estimates") {
            if (params.estimateId) {
              segments.push({ label: "Kosztorysy", path: `/projects/${params.projectId}/cost-estimates` });
              segments.push({ label: "Edycja kosztorysu", path: location.pathname, isCurrentPage: true });
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
        segments.push({ label: "Szablony kosztorysów", path: "/cost-estimate-templates", isCurrentPage: true });
      } else if (pathSegments[0] === "profile") {
        segments.push({ label: "Ustawienia", path: "/profile", isCurrentPage: true });
      } else if (pathSegments[0] === "dashboard") {
        segments[0].isCurrentPage = true;
      }

      setBreadcrumbs(segments);
    };

    generateBreadcrumbs();
  }, [location.pathname, params, projectName]);

  if (breadcrumbs.length <= 1) {
    return null;
  }

  return (
    <Box 
      px={{ base: 4, md: 10 }} 
      py={3} 
      borderBottom="1px solid" 
      borderColor={borderColor}
      bg={bgColor}
    >
      <Breadcrumb spacing={2} separator={<ChevronRight size={16} />}>
        {breadcrumbs.map((crumb, index) => (
          <BreadcrumbItem key={index} isCurrentPage={crumb.isCurrentPage}>
            {crumb.isCurrentPage ? (
              <BreadcrumbLink fontWeight="semibold" color="blue.600">
                {crumb.label}
              </BreadcrumbLink>
            ) : (
              <BreadcrumbLink as={Link} to={crumb.path}>
                {crumb.label}
              </BreadcrumbLink>
            )}
          </BreadcrumbItem>
        ))}
      </Breadcrumb>
    </Box>
  );
}
