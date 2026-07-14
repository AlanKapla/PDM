import { useEffect } from "react";
import { useToastNotification } from "../../hooks/useToastNotification";
import { registerApiErrorListener } from "../../utils/apiErrorToastBridge";

/**
 * Podłącza globalny mostek toastów dla błędów API raportowanych poza komponentami React.
 */
export function ApiErrorToastBridge(): null {
  const { showApiError } = useToastNotification();

  useEffect(() => {
    registerApiErrorListener(showApiError);
    return () => {
      registerApiErrorListener(null);
    };
  }, [showApiError]);

  return null;
}
