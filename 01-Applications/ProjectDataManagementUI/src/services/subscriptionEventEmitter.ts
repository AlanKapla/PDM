type SubscriptionBlockedPayload = {
  tenantId: string;
  isAdmin: boolean;
};

const SUBSCRIPTION_BLOCKED_EVENT = 'subscription:blocked';

class SubscriptionEventEmitter extends EventTarget {
  emitBlocked(payload: SubscriptionBlockedPayload): void {
    this.dispatchEvent(
      new CustomEvent(SUBSCRIPTION_BLOCKED_EVENT, { detail: payload })
    );
  }

  onBlocked(handler: (payload: SubscriptionBlockedPayload) => void): () => void {
    const listener = (e: Event) => {
      handler((e as CustomEvent<SubscriptionBlockedPayload>).detail);
    };
    this.addEventListener(SUBSCRIPTION_BLOCKED_EVENT, listener);
    return () => this.removeEventListener(SUBSCRIPTION_BLOCKED_EVENT, listener);
  }
}

export const subscriptionEventEmitter = new SubscriptionEventEmitter();
