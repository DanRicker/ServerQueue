using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Drp.ServerQueueData;
using Drp.Types;
using Drp;

using DrpServerQueueUser.Actions;

namespace DrpServerQueueUser
{
    class Program
    {

        private static ActionGroupType EnsureActionGroupType(string[] args)
        {
            ActionGroupType ret = ActionGroupType.EnqueueAtoBtoCtoDtoEtoFtoDequeue;
            // ActionGroupType ret = ActionGroupType.EnqueueABCDEF;
            if (args.Length == 1 && false == string.IsNullOrWhiteSpace(args[0]))
            {
                string argString = args[0].Substring(0, 1).ToUpper();
                switch(argString)
                {
                    case "E":
                        {
                            ret = ActionGroupType.EnqueueABCDEF;
                            break;
                        }
                    case "D":
                        {
                            ret = ActionGroupType.AcquireDequeueABCDEF;
                            break;
                        }
                    case "R":
                        {
                            ret = ActionGroupType.EnqueueAtoBtoCtoDtoEtoFtoDequeue;
                            break;
                        }
                    case "A":
                        {
                            ret = ActionGroupType.AcquireDequeueAny;
                            break;
                        }
                    case "B":
                        {
                            ret = ActionGroupType.EnqueueItemTypeBlank;
                            break;
                        }
                    default:
                        {
                            int argAction = 0;
                            if (int.TryParse(args[0], out argAction))
                            {
                                switch (argAction)
                                {
                                    case 0:
                                        {
                                            ret = ActionGroupType.EnqueueABCDEF;
                                            break;
                                        }
                                    case 1:
                                        {
                                            ret = ActionGroupType.AcquireDequeueABCDEF;
                                            break;
                                        }
                                    case 2:
                                        {
                                            ret = ActionGroupType.EnqueueAtoBtoCtoDtoEtoFtoDequeue;
                                            break;
                                        }
                                    case 4:
                                        {
                                            ret = ActionGroupType.AcquireDequeueAny;
                                            break;
                                        }
                                    case 5:
                                        {
                                            ret = ActionGroupType.EnqueueItemTypeBlank;
                                            break;
                                        }
                                }
                            }
                            break;
                        }
                }
            }
            return ret;
        }

        static void Main(string[] args)
        {
            //RunEnqueue();
            RunActionGroup(args);
            //RunAcquireDequeue("AAAAA");
            //RunActionGroup(ActionGroupType.EnqueueABCDEF);

        }


        private static void RunActionGroup(string[] args)
        {

            ActionGroupType actionGroupType = EnsureActionGroupType(args);
            RunActionGroup(actionGroupType);
        }


        private static void RunActionGroup(ActionGroupType actionGroupType)
        {
            ActionGroup actionGroup = ActionGroup.GetActionGroup(actionGroupType);
            DateTimeOffset start = DateTimeOffset.UtcNow;
            actionGroup.ContinouelyExecuteActions();
            Console.WriteLine(string.Format("Drp Process Instance ID: {0}", DrpDebugging.DrpProcessInstanceId));
            Console.WriteLine("Executing Action Group: {0}", actionGroupType.ToString());
            Console.Write("Press any key to quit: ");
            ConsoleKeyInfo keyInfo = Console.ReadKey();
            Console.WriteLine();
            Console.WriteLine("Executed Time: {0}", (DateTimeOffset.UtcNow - start).ToString(@"'['ddd'].['hh\:mm\:ss']'"));
            string temp = keyInfo.Key.ToString();
        }


        private static void RunEnqueue(string itemType = "AAAAA")
        {
            ActionBase testAction = new ActionEnqueue("Enqueue AAAAA")
            {
                ItemType = itemType
            };

            ActionGroup actionGroup = new ActionGroup() { Actions = new List<ActionBase>(new ActionBase[] { testAction }) };
            actionGroup.ContinouelyExecuteActions();
            DateTimeOffset start = DateTimeOffset.UtcNow;
            
            Console.WriteLine(string.Format("Drp Process Instance ID: {0}", DrpDebugging.DrpProcessInstanceId));
            Console.WriteLine("Executing Action Enequeue {0}", itemType);
            Console.Write("Press any key to quit: ");
            ConsoleKeyInfo keyInfo = Console.ReadKey();
            Console.WriteLine();
            Console.WriteLine("Executed Time: {0}", (DateTimeOffset.UtcNow - start).ToString(@"'['ddd'].['hh\:mm\:ss']'"));
            string temp = keyInfo.Key.ToString();
        }


        private static void RunAcquireDequeue(string itemType = "AAAAA")
        {
            ActionBase testAction = new ActionAcquireDequeue("TestAcquireDequeue")
            {
                ItemType = itemType,
            };
            // IServerQueue serverQueue = new ServerQueue(Properties.Settings.Default.DefaultConnectionString);
            // IDrpQueueItem queueItem = serverQueue.Acquire("TestAcquire", "AAAAA");

            ActionGroup actionGroup = new ActionGroup() { Actions = new List<ActionBase>(new ActionBase[] { testAction }) };
            actionGroup.ContinouelyExecuteActions();
            DateTimeOffset start = DateTimeOffset.UtcNow;

            Console.WriteLine(string.Format("Drp Process Instance ID: {0}", DrpDebugging.DrpProcessInstanceId));
            Console.WriteLine("Executing Action Acquire Dequeue {0}", itemType);
            Console.Write("Press any key to quit: ");
            ConsoleKeyInfo keyInfo = Console.ReadKey();
            Console.WriteLine();
            Console.WriteLine("Executed Time: {0}", (DateTimeOffset.UtcNow - start).ToString(@"'['ddd'].['hh\:mm\:ss']'"));
            string temp = keyInfo.Key.ToString();
        }

    }
}
