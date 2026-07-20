import React, { useState } from "react";
import {
  Alert,
  AlertIcon,
  Box,
  Button,
  FormControl,
  FormErrorMessage,
  FormHelperText,
  FormLabel,
  Heading,
  Input,
  SimpleGrid,
  Textarea,
  useColorModeValue,
  useDisclosure,
  VStack,
} from "@chakra-ui/react";
import { Send } from "lucide-react";
import DeleteAlertDialog from "../ui/DeleteAlertDialog";
import type { SendColdMailsRequest } from "../../types/admin.types";
import {
  ColdMailBodyEditor,
  isColdMailBodyEmpty,
} from "./ColdMailBodyEditor";
import { ColdMailTemplatePreview } from "./ColdMailTemplatePreview";

const MAX_EMAILS = 50;
const MAX_SUBJECT_LENGTH = 500;
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export interface ColdMailSendFormProps {
  onSubmit: (request: SendColdMailsRequest) => Promise<void>;
  isSubmitting: boolean;
}

interface FormErrors {
  emails: string | null;
  subject: string | null;
  body: string | null;
}

function parseAndValidateEmails(raw: string): {
  emails: string[];
  error: string | null;
} {
  const lines: string[] = raw
    .split(/\r?\n/)
    .map((line: string) => line.trim())
    .filter((line: string) => line.length > 0);

  const unique: string[] = [];
  const seen: Set<string> = new Set();

  for (const line of lines) {
    const normalized: string = line.toLowerCase();
    if (seen.has(normalized)) {
      continue;
    }
    seen.add(normalized);
    unique.push(line);
  }

  if (unique.length === 0) {
    return { emails: [], error: "Podaj co najmniej jeden adres e-mail." };
  }

  if (unique.length > MAX_EMAILS) {
    return {
      emails: [],
      error: `Maksymalnie ${MAX_EMAILS} adresów e-mail na jedną wysyłkę.`,
    };
  }

  const invalid: string[] = unique.filter(
    (email: string) => !EMAIL_REGEX.test(email)
  );

  if (invalid.length > 0) {
    return {
      emails: [],
      error: `Nieprawidłowy format e-mail: ${invalid.slice(0, 3).join(", ")}${
        invalid.length > 3 ? "…" : ""
      }`,
    };
  }

  return { emails: unique, error: null };
}

export function ColdMailSendForm({
  onSubmit,
  isSubmitting,
}: ColdMailSendFormProps): React.ReactElement {
  const confirmDisclosure = useDisclosure();
  const cardBg = useColorModeValue("white", "gray.800");
  const borderColor = useColorModeValue("gray.200", "gray.700");
  const [emailsText, setEmailsText] = useState<string>("");
  const [subject, setSubject] = useState<string>("");
  const [body, setBody] = useState<string>("");
  const [errors, setErrors] = useState<FormErrors>({
    emails: null,
    subject: null,
    body: null,
  });
  const [pendingRequest, setPendingRequest] =
    useState<SendColdMailsRequest | null>(null);

  const validate = (): SendColdMailsRequest | null => {
    const { emails, error: emailsError } = parseAndValidateEmails(emailsText);
    const trimmedSubject: string = subject.trim();
    const bodyEmpty: boolean = isColdMailBodyEmpty(body);

    const nextErrors: FormErrors = {
      emails: emailsError,
      subject: !trimmedSubject
        ? "Temat jest wymagany."
        : trimmedSubject.length > MAX_SUBJECT_LENGTH
          ? `Temat nie może przekraczać ${MAX_SUBJECT_LENGTH} znaków.`
          : null,
      body: bodyEmpty ? "Treść jest wymagana." : null,
    };

    setErrors(nextErrors);

    if (nextErrors.emails || nextErrors.subject || nextErrors.body) {
      return null;
    }

    return {
      emails,
      subject: trimmedSubject,
      body,
    };
  };

  const handleRequestSend = (): void => {
    const request: SendColdMailsRequest | null = validate();
    if (!request) {
      return;
    }
    setPendingRequest(request);
    confirmDisclosure.onOpen();
  };

  const handleConfirmSend = async (): Promise<void> => {
    if (!pendingRequest) {
      return;
    }

    try {
      await onSubmit(pendingRequest);
      setEmailsText("");
      setSubject("");
      setBody("");
      setErrors({ emails: null, subject: null, body: null });
      setPendingRequest(null);
      confirmDisclosure.onClose();
    } catch {
      // Błąd toastowany w hooku mutacji
    }
  };

  const handleCloseConfirm = (): void => {
    if (isSubmitting) {
      return;
    }
    confirmDisclosure.onClose();
    setPendingRequest(null);
  };

  const recipientCount: number = pendingRequest?.emails.length ?? 0;

  return (
    <>
      <SimpleGrid columns={{ base: 1, lg: 2 }} spacing={6} alignItems="stretch">
        <Box
          as="section"
          aria-labelledby="cold-mail-send-heading"
          bg={cardBg}
          borderWidth="1px"
          borderColor={borderColor}
          borderRadius="xl"
          p={{ base: 4, md: 6 }}
        >
          <Heading id="cold-mail-send-heading" size="md" mb={4}>
            Nowa wysyłka
          </Heading>

          <VStack align="stretch" spacing={4}>
            <FormControl isRequired isInvalid={!!errors.emails}>
              <FormLabel>Adresy e-mail</FormLabel>
              <Textarea
                value={emailsText}
                onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) =>
                  setEmailsText(e.target.value)
                }
                placeholder={"prospect@firma.pl\ninny@firma.pl"}
                rows={5}
                aria-describedby="cold-mail-emails-help"
              />
              <FormHelperText id="cold-mail-emails-help">
                Jeden adres na linię. Maksymalnie {MAX_EMAILS} adresów (duplikaty
                zostaną usunięte).
              </FormHelperText>
              {errors.emails && (
                <FormErrorMessage role="alert">{errors.emails}</FormErrorMessage>
              )}
            </FormControl>

            <FormControl isRequired isInvalid={!!errors.subject}>
              <FormLabel>Temat</FormLabel>
              <Input
                value={subject}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                  setSubject(e.target.value)
                }
                placeholder="Temat wiadomości"
                maxLength={MAX_SUBJECT_LENGTH}
              />
              {errors.subject && (
                <FormErrorMessage role="alert">{errors.subject}</FormErrorMessage>
              )}
            </FormControl>

            <FormControl isRequired isInvalid={!!errors.body}>
              <FormLabel>Treść</FormLabel>
              <ColdMailBodyEditor
                value={body}
                onChange={setBody}
                isInvalid={!!errors.body}
                aria-describedby={errors.body ? "cold-mail-body-error" : undefined}
              />
              <FormHelperText>
                Formatowanie (nagłówek, pogrubienie, listy, linki) zostanie
                zachowane w mailu.
              </FormHelperText>
              {errors.body && (
                <FormErrorMessage id="cold-mail-body-error" role="alert">
                  {errors.body}
                </FormErrorMessage>
              )}
            </FormControl>

            {(errors.emails || errors.subject || errors.body) && (
              <Alert status="error" role="alert" borderRadius="md">
                <AlertIcon aria-hidden="true" />
                Popraw błędy formularza przed wysyłką.
              </Alert>
            )}

            <Box>
              <Button
                leftIcon={<Send size={16} aria-hidden="true" />}
                colorScheme="primary"
                onClick={handleRequestSend}
                isLoading={isSubmitting}
              >
                Wyślij
              </Button>
            </Box>
          </VStack>
        </Box>

        <ColdMailTemplatePreview subject={subject} body={body} />
      </SimpleGrid>

      <DeleteAlertDialog
        isOpen={confirmDisclosure.isOpen}
        onClose={handleCloseConfirm}
        onConfirm={() => {
          void handleConfirmSend();
        }}
        title="Zakolejkować cold maile?"
        description={
          recipientCount === 1
            ? "Wiadomość zostanie zakolejkowana do 1 odbiorcy."
            : `Wiadomość zostanie zakolejkowana do ${recipientCount} odbiorców.`
        }
        confirmLabel="Wyślij"
        isLoading={isSubmitting}
      />
    </>
  );
}
