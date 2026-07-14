namespace Business.Implementation.Utilities
{
    public static class UtcDateTimeHelper
    {
        public static DateTimeOffset ToUtcOffset(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return new DateTimeOffset(dateTime);
            }

            if (dateTime.Kind == DateTimeKind.Local)
            {
                return dateTime.ToUniversalTime();
            }

            return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
        }

        public static DateTime SpecifyUtc(DateTime dateTime)
        {
            if (dateTime == default)
            {
                return DateTime.UtcNow;
            }

            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return dateTime;
            }

            if (dateTime.Kind == DateTimeKind.Local)
            {
                return dateTime.ToUniversalTime();
            }

            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }
    }
}
