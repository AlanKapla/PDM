import { exceptionMessages, defaultErrorMessage } from "./errorMessages";

export const handleApiError = async (response: Response) => {
  let data: any = null;

  try {
    data = await response.json();
  } catch {
  }

  if (data?.type && exceptionMessages[data.type]) {
    return data.message || exceptionMessages[data.type];
  }

  if (data?.errorType === "ValidationApiException" && data.errors) {
    const messages = Object.values(data.errors).flat();
    return messages.join("\n");
  }

  if (data?.message) {
    return data.message;
  }

  if (exceptionMessages[response.status]) {
    return exceptionMessages[response.status];
  }

  return defaultErrorMessage;
};
