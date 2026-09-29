namespace TaskManagerApi.Models
{
    public class EmailReminder : NotificationBase
    {
        public override void SendReminder(string taskTitle)
        {
            Console.WriteLine($"Email sent: Task '{taskTitle}' is completed!");
        }
    } 
}