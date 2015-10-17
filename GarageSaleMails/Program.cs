using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Net.Mail;
using SendGrid;
using System.Net;

namespace GarageSaleMails
{
    // To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976
    public class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main(string[] args)
        {

            var host = new JobHost();

            switch (args.Length)
            {
                case 0:
                  var method1 = typeof(Program).GetMethod("SendNotifications");
                    host.Call(method1, new { dummy = "dummy" });
                    break;
                case 1:
                    var method2 = typeof(Program).GetMethod("SendInvites");
                    host.Call(method2, new { dummy = "dummy" });
                    break;
            }

           


            //var host = new JobHost();
            //// The following code ensures that the WebJob will be running continuously
            //host.RunAndBlock();
        }

        [NoAutomaticTrigger]
        public static void SendNotifications(string dummy)
        {
            string emailaddress, subject, mytemplate = "";

            Console.Write("Waypoint 1");

            GarageSaleDataContext db = new GarageSaleDataContext();

            var linqMailTemplates = db.sp_getTemplate(1);
            foreach (var linqMailTemplate in linqMailTemplates)
            {
                mytemplate = linqMailTemplate.TemplateText;
            }

            int itemid = 0;
            string conversation = "";
            var linqMembers = db.sp_whatMailToSent();
            foreach (var linqMember in linqMembers)
            {
                var sendtemplate = mytemplate;
                emailaddress = linqMember.EmailAddress;
                subject = linqMember.ItemName;

                if (linqMember.ItemID != itemid)
                {
                    conversation = "";
                    var linqMessages = db.sp_EmailDetails(linqMember.ItemID);
                    foreach (var linqMessage in linqMessages)
                    {
                        if (linqMessage.Seller == 1)
                        {
                            conversation += "<b>Seller</b>" + linqMessage.DateInserted + "<br />";
                        }
                        else
                        {
                            conversation += "<b>Buyer</b>" + linqMessage.DateInserted + "<br />";
                        }
                        conversation += linqMessage.MessageText;
                        conversation += "<br /><br />";
                    }
                    itemid = Convert.ToInt32(linqMember.ItemID);
                }

                sendtemplate = sendtemplate.Replace("#Conversation", conversation);
                sendtemplate = sendtemplate.Replace("#ItemName", linqMember.ItemName);

                var linqUpdateEmailTrack = db.sp_UpdateEmailTrack(itemid, emailaddress);

                SendMail(emailaddress, subject, sendtemplate);
            }
        }

        [NoAutomaticTrigger]
        public static async void SendMail(string emailaddress, string subject, string mytemplate)
        {
            Console.Write("emailaddress = " + emailaddress);
            Console.Write("subject = " + subject);
            Console.Write("mytemplate = " + mytemplate);

            // Create the email object first, then add the properties.
            var myMessage = new SendGridMessage();

            // Add the message properties.
            myMessage.From = new MailAddress("noreply@virtualgaragesales.co.za", "VirtualGarageSales");

            // Add multiple addresses to the To field.
            List<String> recipients = new List<String>
            {
                emailaddress
            };


            myMessage.AddTo(recipients);
            myMessage.AddBcc("rossouw.daniel@gmail.com");

            myMessage.Subject = subject;

            //Add the HTML and Text bodies
            string template = mytemplate;

            myMessage.Html = template;
            myMessage.Text = "Hello World plain text!";

            // Create network credentials to access your SendGrid account.
            var username = "azure_00db54dbccd19ec61af8bb92193f66ca@azure.com";
            var pswd = "Lappies12";

            var credentials = new NetworkCredential(username, pswd);

            // Create an Web transport for sending email, using credentials...
            var transportWeb = new Web(credentials);

            // Send the email. 
            try
            {
                await transportWeb.DeliverAsync(myMessage);
                Console.WriteLine("Sent!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR = " + ex.Message);
            }
        }
    }

}
