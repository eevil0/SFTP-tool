using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;
using System.Collections.Specialized;
using System.Net.Mail;
using WinSCP;

namespace SFTP_tool
{
    class Program
    {
        static void Main(string[] args)
        {
         //variables
            //sftp
            string hostName;
            string userName;
            string passWord;
            int sftpPortNumber;
            string sshhkeyfPrint;
            string sourceDirPush;
            string destDirPush;
            string sourceDirPull;
            string destDirPull;
            //mail
            string mailServerName;
            int mailPortNumber;
            string mailFromAddr;
            string mailSubject;
            
                int n;
                int i;
                string chktxt;
                bool stop = false;

                for (n = 0; stop == false; n++)
                {
                    chktxt = ConfigurationManager.AppSettings.Get("mailtoaddress" + n);
                    Console.WriteLine(chktxt);
                    if (chktxt == "")
                    {
                        stop = true;
                    }
                }
                i = n - 1;

            string[] mailToAddr = new string[i];
            string mailErrorMessage;
            //misc
            string logDir;
            bool transferTypePull = false;
            bool transferTypePush = false;
            
            //get config info
            mailServerName = ConfigurationManager.AppSettings.Get("mailservername");
            mailPortNumber = Convert.ToInt16(ConfigurationManager.AppSettings.Get("mailportnumber"));
            mailFromAddr = ConfigurationManager.AppSettings.Get("mailfromaddress");
            mailToAddr[0] = ConfigurationManager.AppSettings.Get("mailtoaddress0");
            mailSubject = ConfigurationManager.AppSettings.Get("mailsubject");            
            hostName = ConfigurationManager.AppSettings.Get("hostname");
            userName = ConfigurationManager.AppSettings.Get("username");
            passWord = ConfigurationManager.AppSettings.Get("password");
            sshhkeyfPrint = ConfigurationManager.AppSettings.Get("sshhkfp");
            sftpPortNumber = Convert.ToInt16(ConfigurationManager.AppSettings.Get("sftpportnumber"));
            sourceDirPush = ConfigurationManager.AppSettings.Get("sourcedirpush");
            destDirPush = ConfigurationManager.AppSettings.Get("destdirpush");
            sourceDirPull = ConfigurationManager.AppSettings.Get("sourcedirpull");
            destDirPull = ConfigurationManager.AppSettings.Get("destdirpull");
            logDir = ConfigurationManager.AppSettings.Get("logfiledir");

         //triggering pull&push process - no pull trigger yet!!!!
            if(chkDir(sourceDirPush) == true &&
                System.IO.Directory.GetFiles(sourceDirPush).Length > 0)
            {
                transferTypePush = true;
            }
            
         //main process: sftp transfer
            if(transferTypePush == true ||
                transferTypePull == true)
            {
                logAction(timeStamp("Transfer process started"));
                try
                {
                    SessionOptions sessionOptions = new SessionOptions
                    {
                        Protocol = Protocol.Sftp,
                        HostName = hostName,
                        UserName = userName,
                        Password = passWord,
                        PortNumber = sftpPortNumber,
                        SshHostKeyFingerprint = sshhkeyfPrint
                    };

                    using (Session session = new Session())
                    {
                        // Connect
                        session.Open(sessionOptions);

                        // Transfer files
                        TransferOptions transferOptions = new TransferOptions();
                        transferOptions.TransferMode = TransferMode.Binary;

                        if (transferTypePush == true)
                        {
                            logAction(timeStamp("Push process triggered."));
                            TransferOperationResult transferResult;
                            transferResult = session.PutFiles(sourceDirPush + "*", destDirPush, false, transferOptions);

                            // Throw on any error
                            transferResult.Check();

                            // Print results
                            foreach (TransferEventArgs transfer in transferResult.Transfers)
                            {
                                Console.WriteLine("Upload of {0} succeeded", transfer.FileName);
                                logAction(timeStamp(transfer.FileName));
                                Console.ReadKey();
                            }
                        }

                        if (transferTypePull == true)
                        {
                            logAction(timeStamp("Pull process triggered."));
                            TransferOperationResult transferResult;
                            transferResult = session.GetFiles(sourceDirPull + "*", destDirPull, false, transferOptions);

                            // Throw on any error
                            transferResult.Check();

                            // Print results
                            foreach (TransferEventArgs transfer in transferResult.Transfers)
                            {
                                Console.WriteLine("Upload of {0} succeeded", transfer.FileName);
                                logAction(timeStamp(transfer.FileName));
                                Console.ReadKey();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Transfer process error: {0}", e);
                    Console.ReadKey();
                    logAction(timeStamp("Transfer process error: " + e.ToString()));

                    try
                    {
                        sendMail("Transfer process error: " + e.ToString());
                    }
                    catch (Exception exceptionMessage)
                    {
                        mailErrorMessage = "Failed to send mail because: " + System.Environment.NewLine + exceptionMessage.ToString();
                        logAction(timeStamp(mailErrorMessage));
                    }
                }
                logAction(timeStamp("Transfer process completed"));
            }

         //log message timestamping
            string timeStamp(string inputString)
            {
                inputString = DateTime.Now.ToString() + " " + inputString + System.Environment.NewLine + System.Environment.NewLine;
                return inputString;
            }

         //initialize log
            string logAction(string actionToLog)
            {
                string logFileName = DateTime.Today.ToString("yyyyMMdd") + ".log";
                logFileName = System.IO.Path.Combine(logDir, logFileName);
                if (System.IO.File.Exists(logFileName))
                {
                    Console.WriteLine("Log file found.");
                }
                else
                {
                    File.Create(logFileName).Close();
                    Console.WriteLine("Log file " + logFileName + " created.");
                }
                File.AppendAllText(logFileName, actionToLog);
                Console.WriteLine("actionToLog = " + actionToLog);
                Console.ReadKey();
                return actionToLog;
            }

         //mail function
            void sendMail(string mailBody)
            {
                SmtpClient client = new SmtpClient(mailServerName, mailPortNumber);
                MailMessage message = new MailMessage(mailFromAddr, mailToAddr[0], mailSubject, mailBody);

                if (mailToAddr.Length > 1)
                {
                    Console.WriteLine("multiple receipients");
                    for (i = 1; i < mailToAddr.Length; i++)
                    {
                        mailToAddr[i] = ConfigurationManager.AppSettings.Get("toAddr" + i);
                        message.To.Add(mailToAddr[i]);
                        Console.WriteLine("recipient added: " + mailToAddr[i]);
                    }
                }
                Console.WriteLine(message.To.ToString());
                //Console.ReadKey();
                client.Send(message);
            }
         //directory exists?
            bool chkDir(string dir)
            {
                if (!System.IO.Directory.Exists(dir))
                {
                    logAction(timeStamp("Folder " + dir.ToString() + "cannot be reached"));
                    return false;
                }
                return true;
            }
        }
    }
}
