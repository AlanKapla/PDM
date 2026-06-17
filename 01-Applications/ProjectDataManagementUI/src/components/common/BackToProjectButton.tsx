import React from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Button } from "@chakra-ui/react";
import { ArrowLeft } from "lucide-react";

export interface BackToProjectButtonProps {
  projectId?: string;
  mb?: number | string;
}

export default function BackToProjectButton({
  projectId,
  mb = 4,
}: BackToProjectButtonProps): React.ReactElement {
  const navigate = useNavigate();
  const params = useParams<{ projectId: string }>();
  const resolvedProjectId = projectId ?? params.projectId;

  return (
    <Button
      leftIcon={<ArrowLeft size={16} aria-hidden="true" />}
      variant="ghost"
      size="sm"
      mb={mb}
      onClick={() => navigate(`/projects/${resolvedProjectId}`)}
    >
      Powrót do projektu
    </Button>
  );
}
