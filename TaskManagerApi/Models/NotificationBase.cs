namespace TaskManagerApi.Models
{
    public abstract class NotificationBase
    {
        public abstract void SendReminder(string taskTitle);
    }
}
