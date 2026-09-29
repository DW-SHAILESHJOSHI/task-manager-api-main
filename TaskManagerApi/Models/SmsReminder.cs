namespace TaskManagerApi.Models
{
    public class SmsReminder : NotificationBase
    {
        public override void SendReminder(string taskTitle)
        {
            Console.WriteLine($"Sms sent : Task '{taskTitle}' is completed!");
        }
    }
}