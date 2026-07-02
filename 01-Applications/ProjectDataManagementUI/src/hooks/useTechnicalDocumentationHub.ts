import { useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../context/AuthContext';
import { useToastNotification } from './useToastNotification';
import { technicalDocumentationHubService } from '../services/technicalDocumentationHubService';
import { technicalDocumentationKeys } from './queries/useTechnicalDocumentation';
import {
  TechnicalDocumentationStatus,
  type TechnicalDocumentationProcessingEvent,
} from '../types/technicalDocumentation.types';

export function useTechnicalDocumentationHub(): void {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToastNotification();
  const { user, isAuthenticated } = useAuth();
  const activeTenantId = user?.activeTenantId;

  useEffect(() => {
    // Connection lifecycle is managed centrally in AuthContext after login.
    if (!isAuthenticated) {
      return;
    }

    const unsubscribe = technicalDocumentationHubService.onProcessingCompleted(
      (event: TechnicalDocumentationProcessingEvent) => {
        if (activeTenantId && event.tenantId !== activeTenantId) {
          return;
        }

        queryClient.invalidateQueries({
          queryKey: technicalDocumentationKeys.list(event.tenantId, event.projectId),
        });
        queryClient.invalidateQueries({
          queryKey: technicalDocumentationKeys.detail(
            event.tenantId,
            event.projectId,
            event.documentationId
          ),
        });
        queryClient.invalidateQueries({
          queryKey: technicalDocumentationKeys.count(event.tenantId, event.projectId),
        });

        if (event.status === TechnicalDocumentationStatus.Completed) {
          showSuccess(
            'Przetwarzanie zakończone',
            `Dokumentacja „${event.name}” została przetworzona pomyślnie.`
          );
          return;
        }

        if (event.status === TechnicalDocumentationStatus.CompletedWithWarnings) {
          showSuccess(
            'Przetwarzanie zakończone z ostrzeżeniami',
            `Dokumentacja „${event.name}” została przetworzona, ale wymaga weryfikacji ostrzeżeń.`
          );
          return;
        }

        if (event.status === TechnicalDocumentationStatus.Failed) {
          showError(
            'Przetwarzanie nie powiodło się',
            event.errorMessage ?? `Dokumentacja „${event.name}” zakończyła się błędem.`
          );
        }
      }
    );

    return () => {
      unsubscribe();
    };
  }, [activeTenantId, isAuthenticated, queryClient, showSuccess, showError]);
}
