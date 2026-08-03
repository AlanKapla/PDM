import { useQuery, useInfiniteQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { notificationApi } from '../../api/notificationApi';
import type { NotificationWeb } from '../../types/notification.types';

export const notificationKeys = {
  all: ['notifications'] as const,
  list: (filter: 'all' | 'unread') =>
    ['notifications', 'list', filter] as const,
  unreadCounter: () => ['notifications', 'unread-counter'] as const,
};

const PAGE_SIZE = 20;

/**
 * Licznik nieprzeczytanych powiadomień
 * Aktualizowany przez SignalR + invalidate po mutacjach
 */
export function useUnreadCounter() {
  return useQuery<number>({
    queryKey: notificationKeys.unreadCounter(),
    queryFn: () => notificationApi.getUnreadCounter(),
    staleTime: 30 * 1000,
    refetchInterval: 30 * 1000,
    refetchIntervalInBackground: true,
  });
}

/**
 * Infinite query dla listy powiadomień (all lub unread)
 * Automatyczna paginacja przez fetchNextPage()
 */
export function useNotificationsInfinite(filter: 'all' | 'unread', enabled: boolean = true) {
  return useInfiniteQuery<NotificationWeb[]>({
    queryKey: notificationKeys.list(filter),
    queryFn: async ({ pageParam = 0 }) => {
      const skip = pageParam as number;
      if (filter === 'all') {
        return notificationApi.getAll(PAGE_SIZE, skip);
      } else {
        return notificationApi.getUnread(PAGE_SIZE, skip);
      }
    },
    initialPageParam: 0,
    getNextPageParam: (lastPage, allPages) => {
      if (lastPage.length < PAGE_SIZE) return undefined;
      return allPages.length * PAGE_SIZE;
    },
    enabled,
  });
}

/**
 * Mutacja: oznacz jako przeczytane
 */
export function useMarkAsRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (notificationId: string) =>
      notificationApi.markAsRead(notificationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notificationKeys.all });
    },
  });
}

/**
 * Mutacja: oznacz wszystkie jako przeczytane
 */
export function useMarkAllAsRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => notificationApi.markAllAsRead(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notificationKeys.all });
    },
  });
}
