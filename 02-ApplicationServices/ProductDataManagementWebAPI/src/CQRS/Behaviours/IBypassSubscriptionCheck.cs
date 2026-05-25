namespace CQRS.Behaviours;

/// <summary>
/// Oznacza komendy/zapytania które są przepuszczane przez SubscriptionEnforcementBehavior
/// niezależnie od statusu subskrypcji tenanta (np. płatność, status subskrypcji).
/// </summary>
public interface IBypassSubscriptionCheck { }
