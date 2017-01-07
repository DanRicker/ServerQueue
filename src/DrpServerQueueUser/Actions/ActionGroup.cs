using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Drp;

namespace DrpServerQueueUser.Actions
{

    internal enum ActionGroupType
    {
        EnqueueABCDEF,
        AcquireDequeueABCDEF,
        EnqueueAtoBtoCtoDtoEtoFtoDequeue,
        EnqueueItemTypeBlank,
        AcquireDequeueAny
    }


    internal class ActionGroup
    {
        public ActionGroupType Type { get; protected set; }

        public List<ActionBase> Actions { get; set; }

        public void ContinouelyExecuteActions()
        {
            foreach (ActionBase action in this.Actions)
            {
                ContinuouslyExecuteAction(action);
            }

        }

        protected async void ContinuouslyExecuteAction(ActionBase action)
        {
            IServerQueue serverQueue = new ServerQueue(Properties.Settings.Default.DefaultConnectionString);

            while (true)
            {
                try
                {
                    if (false == await action.ExecuteAsync(serverQueue))
                    {
                        DrpDebugging.DebugWriteLine(string.Format("Failure: {0}", action.ToString()));
                    }
                    else
                    {
                        DrpDebugging.DebugWriteLine(string.Format("Success: {0}", action.ToString()));
                    }
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine(string.Format(
                        "Program.ExecuteAction Action [{0}]{1}Exception: {2}",
                        action.ToString(),
                        Environment.NewLine,
                        ex.ToString()));
                }

            }
        }

        #region Static Group Creation Methods

        public static ActionGroup GetActionGroup(ActionGroupType type)
        {
            switch(type)
            {
                case ActionGroupType.EnqueueItemTypeBlank:
                case ActionGroupType.AcquireDequeueAny:
                    return new ActionGroup() { Actions = MakeAcquireDequeueAnyList() };
                case ActionGroupType.AcquireDequeueABCDEF:
                    return new ActionGroup() { Actions = MakeAcquireDequeueABCDEFList() };
                case ActionGroupType.EnqueueAtoBtoCtoDtoEtoFtoDequeue:
                    return new ActionGroup() { Actions = MakeEnqueueAtoBtoCtoDtoEtoFtoDequeueList() };
                case ActionGroupType.EnqueueABCDEF:
                default:
                    return new ActionGroup() { Actions = MakeEnqueueABCDEFList() };
            }
        }

        private static string MakeTypeName(string itemTypeChar)
        {
            return new string(itemTypeChar[0], 5);
        }

        private static string MakeActionId(string action, string typeChar, string uniquifier)
        {
            return string.Format("[{0}].[{1}].[{2}]", action, typeChar, uniquifier);
        }

        private static List<ActionBase> MakeEnqueueABCDEFList()
        {
            string idUniqifier = Guid.NewGuid().ToStringDashes();
            return new List<ActionBase>(
                new ActionBase[]
                {
                    new ActionEnqueue(MakeActionId("Enqueue", "A", idUniqifier))
                    {
                        ItemType = MakeTypeName("A")
                    },
                    new ActionEnqueue(MakeActionId("Enqueue", "B", idUniqifier))
                    {
                        ItemType = MakeTypeName("B")
                    },
                    new ActionEnqueue(MakeActionId("Enqueue", "C", idUniqifier))
                    {
                        ItemType = MakeTypeName("C")
                    },
                    new ActionEnqueue(MakeActionId("Enqueue", "D", idUniqifier))
                    {
                        ItemType = MakeTypeName("D")
                    },
                    new ActionEnqueue(MakeActionId("Enqueue", "E", idUniqifier))
                    {
                        ItemType = MakeTypeName("E")
                    },
                    new ActionEnqueue(MakeActionId("Enqueue", "F", idUniqifier))
                    {
                        ItemType = MakeTypeName("F")
                    },
                }
                );
        }

        private static List<ActionBase> MakeAcquireDequeueABCDEFList()
        {
            string idUniqifier = Guid.NewGuid().ToStringDashes();
            return new List<ActionBase>(
                new ActionBase[]
                {
                    new ActionAcquireDequeue(MakeActionId("AcquireDequeue", "A", idUniqifier))
                    {
                        ItemType = MakeTypeName("A")
                    },
                    new ActionAcquireDequeue(MakeActionId("AcquireDequeue", "B", idUniqifier))
                    {
                        ItemType = MakeTypeName("B")
                    },
                    new ActionAcquireDequeue(MakeActionId("AcquireDequeue", "C", idUniqifier))
                    {
                        ItemType = MakeTypeName("C")
                    },
                    new ActionAcquireDequeue(MakeActionId("AcquireDequeue", "D", idUniqifier))
                    {
                        ItemType = MakeTypeName("D")
                    },
                    new ActionAcquireDequeue(MakeActionId("AcquireDequeue", "E", idUniqifier))
                    {
                        ItemType = MakeTypeName("E")
                    },
                    new ActionAcquireDequeue(MakeActionId("AcquireDequeue", "F", idUniqifier))
                    {
                        ItemType = MakeTypeName("F")
                    },
                }
                );
        }

        // EnqueueAtoBtoCtoDtoEtoFtoDequeue
        private static List<ActionBase> MakeEnqueueAtoBtoCtoDtoEtoFtoDequeueList()
        {
            string idUniqifier = Guid.NewGuid().ToStringDashes();
            return new List<ActionBase>(
                new ActionBase[]
                {
                    new ActionEnqueue(MakeActionId("RequeueEnqueueA", "A", idUniqifier))
                    {
                        ItemType = MakeTypeName("A")
                    },
                    new ActionAcquireDequeueEnqueueNew(MakeActionId("RequeueAtoB", "A", idUniqifier))
                    {
                        ItemType = MakeTypeName("A"),
                        NewEnqueueItemType= MakeTypeName("B")
                    },
                    new ActionAcquireDequeueEnqueueNew(MakeActionId("RequeueBtoC", "B", idUniqifier))
                    {
                        ItemType = MakeTypeName("B"),
                        NewEnqueueItemType= MakeTypeName("C")
                    },
                    new ActionAcquireDequeueEnqueueNew(MakeActionId("RequeueCtoD", "C", idUniqifier))
                    {
                        ItemType = MakeTypeName("C"),
                        NewEnqueueItemType= MakeTypeName("D")
                    },
                    new ActionAcquireDequeueEnqueueNew(MakeActionId("RequeueDtoE", "D", idUniqifier))
                    {
                        ItemType = MakeTypeName("D"),
                        NewEnqueueItemType = MakeTypeName("E")
                    },
                    new ActionAcquireDequeueEnqueueNew(MakeActionId("RequeueEtoF", "E", idUniqifier))
                    {
                        ItemType = MakeTypeName("E"),
                        NewEnqueueItemType= MakeTypeName("F")
                    },
                    new ActionAcquireDequeue(MakeActionId("RequeueFtoDequeue", "F", idUniqifier))
                    {
                        ItemType = MakeTypeName("F")
                    },
                }
                );
        }

        private static List<ActionBase> MakeAcquireDequeueAnyList()
        {
            string idUniqifier = Guid.NewGuid().ToStringDashes();
            int index = 0;
            return new List<ActionBase>(
                new ActionBase[]
                {
                    new ActionAcquireDequeue(MakeActionId(
                        "AcquireDequeue",
                        string.Format("Any{0}",(index++).ToString("00")), 
                        idUniqifier)),
                    new ActionAcquireDequeue(MakeActionId(
                        "AcquireDequeue",
                        string.Format("Any{0}",(index++).ToString("00")),
                        idUniqifier)),
                    new ActionAcquireDequeue(MakeActionId(
                        "AcquireDequeue",
                        string.Format("Any{0}",(index++).ToString("00")),
                        idUniqifier)),
                    new ActionAcquireDequeue(MakeActionId(
                        "AcquireDequeue",
                        string.Format("Any{0}",(index++).ToString("00")),
                        idUniqifier)),
                    new ActionAcquireDequeue(MakeActionId(
                        "AcquireDequeue",
                        string.Format("Any{0}",(index++).ToString("00")),
                        idUniqifier)),
                    new ActionAcquireDequeue(MakeActionId(
                        "AcquireDequeue",
                        string.Format("Any{0}",(index++).ToString("00")),
                        idUniqifier)),
                }
                );
        }


        private static List<ActionBase> MakeEnqueueItemTypeBlankList()
        {
            string idUniqifier = Guid.NewGuid().ToStringDashes();
            int index = 0;
            return new List<ActionBase>(
                new ActionBase[]
                {
                    new ActionEnqueue(MakeActionId(
                        "Enqueue",
                        string.Format("Blank{0}",(index++).ToString("00")),
                        idUniqifier)),
                    new ActionEnqueue(MakeActionId(
                        "Enqueue",
                        string.Format("Blank{0}",(index++).ToString("00")),
                        idUniqifier)),
                    new ActionEnqueue(MakeActionId(
                        "Enqueue",
                        string.Format("Blank{0}",(index++).ToString("00")),
                        idUniqifier)),
                }
                );
        }



        #endregion
    }
}
