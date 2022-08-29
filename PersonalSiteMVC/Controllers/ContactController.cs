using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Configuration; //Added for access to info in appsettings.json

using PersonalSiteMVC.Models;

using MimeKit; //Added for access to MimeMessage class

using MailKit.Net.Smtp; //Added for access to SmtpClient class

namespace PersonalSiteMVC.Controllers
{
    public class ContactController : Controller
    {
        //Email - Step 3
        //field
        private readonly IConfiguration _config;

        //ctor 
        //Email - Step 4
        public ContactController(IConfiguration config)
        {
            _config = config;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(ContactViewModel cvm)
        {
            //When a class has validation attributes, that validation should be checked
            //BEFORE attempting to process any of the data they provided.

            if (!ModelState.IsValid)
            {
                //Send them back to the form. We can pass the object to the View
                //so the form will contain the original information they provided.

                return View(cvm);
            }

            //To handle sending the email, we'll need to install a NuGet Package 
            //and add a few using statements. We can do this with the following steps:          

            //Create the format for the message content we will receive from the contact form
            string message = $"You have received a new email from your site's contact form!<br />" +
                $"Sender: {cvm.Name}<br />Email: {cvm.Email}<br />Subject: {cvm.Subject}<br />" +
                $"Message: {cvm.Message}";

            //Create a MimeMessage object to assist with storing/transporting the email 
            //information from the contact form
            var mm = new MimeMessage();

            //Even though the user is the one attempting to send a message to us, the 
            //actual sender of the email is the email user we set up with our hosting provider.

            //We can access the credentials for this email user from our appsettings.json file
            //as shown below.

            mm.From.Add(new MailboxAddress("User", _config.GetValue<string>("Credentials:Email:User")));

            //The recipient of this email will be our personal email address, which is also 
            //stored in appsettings.json

            mm.To.Add(new MailboxAddress("Personal", _config.GetValue<string>("Credentials:Email:Recipient")));

            //The subject will be the one provided by the user, which is stored in the cvm object
            mm.Subject = cvm.Subject;

            //The body of the message will be formatted with the string we created above.
            mm.Body = new TextPart("HTML") { Text = message };

            //We can set the priority of the message as "urgent" so it will be flagged in our mailbox.
            mm.Priority = MessagePriority.Urgent;

            //We can also add the user's provided email address to the list of ReplyTo addresses 
            //so our replies can be sent directly to them (instead of sending to our own email user).
            mm.ReplyTo.Add(new MailboxAddress("Sender", cvm.Email));

            //The using directive will create the SmtpClient object, which is used to send the email.
            //Once all of the code inside the using directive's scope has been executed, it will 
            //automatically close any open connections and dispose of the object for us.

            using (var client = new SmtpClient())
            {
                //Connect to the mail server using the credentials in our appsettings.json
                client.Connect(_config.GetValue<string>("Credentials:Email:Client"));

                //Log in to mail server using the credentials for our email user
                client.Authenticate(

                    //Username
                    _config.GetValue<string>("Credentials:Email:User"),

                    //Password
                    _config.GetValue<string>("Credentials:Email:Password")

                    );

                //It's possible the mail server may be down when the user attempts to contact us.
                //So, we can "encapsulate" our code to send the message in a try/catch.
                try
                {
                    //Try to send the email
                    client.Send(mm);
                }
                catch (Exception ex)
                {
                    //If there is an issue, we can store an error message in the ViewBag
                    //to be displayed in the View
                    ViewBag.ErrorMessage = $"There was an error processing your request. Please try " +
                        $"again later.<br />Error Message: {ex.StackTrace}";

                    //Return the user to the View with their form information intact
                    return View(cvm);
                }

            }

            //If all goes well, return a View that displays a confirmation to the user that 
            //their email was sent successfully.

            return View("EmailConfirmation", cvm);
        }
    }//end class
}//end namespace
