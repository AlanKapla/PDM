import { memo } from "react";
import { Alert, AlertIcon, AlertTitle, AlertDescription, Box } from "@chakra-ui/react";

interface ErrorAlertProps {
  title?: string;
  description?: string;
  variant?: "subtle" | "solid" | "left-accent" | "top-accent";
}

const ErrorAlert = memo(function ErrorAlert({ 
  title = "Błąd", 
  description,
  variant = "left-accent"
}: ErrorAlertProps) {
  return (
    <Alert status="error" variant={variant} borderRadius="md">
      <AlertIcon />
      <Box>
        <AlertTitle>{title}</AlertTitle>
        {description && <AlertDescription>{description}</AlertDescription>}
      </Box>
    </Alert>
  );
});

export default ErrorAlert;
