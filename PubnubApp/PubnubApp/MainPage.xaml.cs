using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using PubnubApi;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace PubnubApp
{
    public partial class MainPage : ContentPage
    {
        PNConfiguration pnConfiguration = new PNConfiguration
        {
            SubscribeKey = "sub-c-286279a8-90ec-11e8-9c3a-c6a250b1bb88",
            PublishKey = "pub-c-a309ec93-215c-4d7f-934a-742ac098d2bd",
            SecretKey = "sec-c-MTJhMWYwMDEtNzlmOS00YjZkLWJiOGEtNDIxMGJmYzlmNDY0",
            LogVerbosity = PNLogVerbosity.BODY,
            Uuid = "PubNubCSharpUser1"
        };

        PNConfiguration pnConfiguration1 = new PNConfiguration
        {
            SubscribeKey = "pub-c-ac69933a-3531-44f0-8fe3-3c45789441cf",
            PublishKey = "sub-c-456d4550-90ec-11e8-9c3a-c6a250b1bb88",
            SecretKey = "sec-c-MzdmMjEyNzQtOTA2NC00NzViLWJkZjktYTg4ODQyN2M1MzNl",
            LogVerbosity = PNLogVerbosity.BODY,
            Uuid = "PubNubCSharpUser2"
        };


        Pubnub pubnub;

        public string MyProperty { get; set; }

        public ObservableCollection<MessageModel> MyList { get; set; }

        public MainPage()
        {
            InitializeComponent();
            //CallingPubnub();

            MyList = new ObservableCollection<MessageModel>();
            DataListView.ItemsSource = MyList;

            pubnub = new Pubnub(pnConfiguration);
            AddListioner();
            Subscribe();

        }

        void CallingPubnub()
        {

            Dictionary<string, string> message = new Dictionary<string, string>();
            message.Add("msg", "Hello World!");

            Pubnub pubnub = new Pubnub(pnConfiguration);

            SubscribeCallbackExt subscribeCallback = new SubscribeCallbackExt(
                (pubnubObj, messageResult) =>
                {
                    if (messageResult != null)
                    {
                        Debug.WriteLine("In Example, SusbcribeCallback received PNMessageResult");
                        Debug.WriteLine("In Example, SusbcribeCallback messsage channel = " + messageResult.Channel);
                        Debug.WriteLine("In Example, SusbcribeCallback messsage channelGroup = " + messageResult.Subscription);
                        Debug.WriteLine("In Example, SusbcribeCallback messsage publishTimetoken = " + messageResult.Timetoken);
                        Debug.WriteLine("In Example, SusbcribeCallback messsage publisher = " + messageResult.Publisher);
                        string jsonString = messageResult.Message.ToString();
                        Dictionary<string, string> msg = pubnub.JsonPluggableLibrary.DeserializeToObject<Dictionary<string, string>>(jsonString);
                        Debug.WriteLine("msg: " + msg["msg"]);

                        // MyList.Add(msg["msg"]);
                    }
                },
                (pubnubObj, presencResult) =>
                {
                    if (presencResult != null)
                    {
                        Debug.WriteLine("In Example, SusbcribeCallback received PNPresenceEventResult");
                        Debug.WriteLine(presencResult.Channel + " " + presencResult.Occupancy + " " + presencResult.Event);
                    }
                },
                (pubnubObj, statusResult) =>
                {
                    if (statusResult.Category == PNStatusCategory.PNConnectedCategory)
                    {
                        pubnub.Publish()
                        .Channel("my_channel")
                        .Message(message)
                        .Async(new PNPublishResultExt((publishResult, publishStatus) =>
                        {
                            if (!publishStatus.Error)
                            {
                                Debug.WriteLine(string.Format("DateTime {0}, In Publish Example, Timetoken: {1}", DateTime.UtcNow, publishResult.Timetoken));
                            }
                            else
                            {
                                Debug.WriteLine(publishStatus.Error);
                                Debug.WriteLine(publishStatus.ErrorData.Information);
                            }
                        }));
                    }
                }
            );

            pubnub.AddListener(subscribeCallback);

            pubnub.Subscribe<string>()
                .Channels(new string[]{
            "my_channel"
                }).Execute();
        }

        void AddListioner()
        {

            SubscribeCallbackExt listenerSubscribeCallack = new SubscribeCallbackExt(
                (pubnubObj, message) =>
                {
                    // Handle new message stored in message.Message 
                    //label.Text = message.Message.ToString();
                    //var model = JsonConvert.DeserializeObject<MessageModel>(message.Message?.ToString());
                    if (message.Message is MessageModel model)
                    {
                        MyList.Add(model);
                    }
                },
                (pubnubObj, presence) =>
                {
                    // handle incoming presence data 

                },
                (pubnubObj, status) =>
                {
                    // the status object returned is always related to subscribe but could contain
                    // information about subscribe, heartbeat, or errors
                    // use the PNOperationType to switch on different options
                    switch (status.Operation)
                    {
                        // let's combine unsubscribe and subscribe handling for ease of use
                        case PNOperationType.PNSubscribeOperation:
                        case PNOperationType.PNUnsubscribeOperation:
                            // note: subscribe statuses never have traditional
                            // errors, they just have categories to represent the
                            // different issues or successes that occur as part of subscribe
                            switch (status.Category)
                            {
                                case PNStatusCategory.PNConnectedCategory:
                                    // this is expected for a subscribe, this means there is no error or issue whatsoever
                                    break;
                                case PNStatusCategory.PNReconnectedCategory:
                                    // this usually occurs if subscribe temporarily fails but reconnects. This means
                                    // there was an error but there is no longer any issue
                                    break;
                                case PNStatusCategory.PNDisconnectedCategory:
                                    // this is the expected category for an unsubscribe. This means there
                                    // was no error in unsubscribing from everything
                                    break;
                                case PNStatusCategory.PNUnexpectedDisconnectCategory:
                                    // this is usually an issue with the internet connection, this is an error, handle appropriately
                                    break;
                                case PNStatusCategory.PNAccessDeniedCategory:
                                    // this means that PAM does allow this client to subscribe to this
                                    // channel and channel group configuration. This is another explicit error
                                    break;
                                default:
                                    // More errors can be directly specified by creating explicit cases for other
                                    // error categories of `PNStatusCategory` such as `PNTimeoutCategory` or `PNMalformedFilterExpressionCategory` or `PNDecryptionErrorCategory`
                                    break;
                            }
                            break;
                        case PNOperationType.PNHeartbeatOperation:
                            // heartbeat operations can in fact have errors, so it is important to check first for an error.
                            if (status.Error)
                            {
                                // There was an error with the heartbeat operation, handle here
                            }
                            else
                            {
                                // heartbeat operation was successful
                            }
                            break;
                        default:
                            // Encountered unknown status type
                            break;
                    }
                });

            pubnub.AddListener(listenerSubscribeCallack);
        }

        void Listener()
        {
            Pubnub pubnub = new Pubnub(pnConfiguration);

            SubscribeCallbackExt listenerSubscribeCallack = new SubscribeCallbackExt(
                (pubnubObj, message) => { },
                (pubnubObj, presence) => { },
                (pubnubObj, status) => { });

            pubnub.AddListener(listenerSubscribeCallack);

            // some time later
            pubnub.RemoveListener(listenerSubscribeCallack);
        }

        void Time()
        {
            Pubnub pubnub = new Pubnub(pnConfiguration);

            pubnub.Time()
    .Async(new PNTimeResultExt(
        (result, status) =>
        {
            // handle time result.
        }
    ));
        }

        void Subscribe()
        {
            pubnub.Subscribe<MessageModel>()
                  .Channels(new string[] {
                // subscribe to channels
                "my_channel"
             })
            .Execute();
        }

        void Publish(MessageModel model)
        {
            pubnub.Publish()
                .Channel("my_channel")
                .Message(model)
                .Async(new PNPublishResultExt(
                    (result, status) =>
                    {
                        // handle publish result, status always present, result if successful
                        // status.Error to see if error happened
                    }
                ));
        }

        void HereNow()
        {
            Pubnub pubnub = new Pubnub(pnConfiguration);

            pubnub.HereNow()
    // tailor the next two lines to example
    .Channels(new string[] {
        "coolChannel",
        "coolChannel2"
     })
    .IncludeUUIDs(true)
    .Async(new PNHereNowResultEx(
        (result, status) =>
        {
            if (status.Error)
            {
                // handle error
                return;
            }

            if (result.Channels != null && result.Channels.Count > 0)
            {
                foreach (KeyValuePair<string, PNHereNowChannelData> kvp in result.Channels)
                {
                    PNHereNowChannelData channelData = kvp.Value;
                    Console.WriteLine("---");
                    Console.WriteLine("channel:" + channelData.ChannelName);
                    Console.WriteLine("occupancy:" + channelData.Occupancy);
                    Console.WriteLine("Occupants:");
                    if (channelData.Occupants != null && channelData.Occupants.Count > 0)
                    {
                        for (int index = 0; index < channelData.Occupants.Count; index++)
                        {
                            PNHereNowOccupantData occupant = channelData.Occupants[index];
                            Console.WriteLine(string.Format("uuid: {0}", occupant.Uuid));
                            Console.WriteLine(string.Format("state:{1}", (occupant.State != null) ?
                            pubnub.JsonPluggableLibrary.SerializeToJsonString(occupant.State) : ""));
                        }
                    }
                }
            }
        }
    ));
        }

        void Presence()
        {
            Pubnub pubnub = new Pubnub(pnConfiguration);

            pubnub.Subscribe<string>()
    .Channels(new string[] {
        // subscribe to channels
        "my_channel"
    })
    .WithPresence() // also subscribe to related presence information
    .Execute();
        }

        void History()
        {
            Pubnub pubnub = new Pubnub(pnConfiguration);

            pubnub.History()
    .Channel("history_channel") // where to fetch history from
    .Count(100) // how many items to fetch
    .Async(new PNHistoryResultExt(
        (result, status) =>
        {
        }
    ));

        }

        void Unsubscribe()
        {
            Pubnub pubnub = new Pubnub(pnConfiguration);

            pubnub.Unsubscribe<string>()
     .Channels(new string[] {
        "my_channel"
     })
     .Execute();
        }

        void Destory()
        {
            Pubnub pubnub = new Pubnub(pnConfiguration);

            pubnub.Destroy();
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            //var data = entryData.Text;
            //CallingPubnub();

            //MyList.Add(entryData.Text);
            //DataListView.ItemsSource = MyList;
            Publish(new MessageModel
            {
                UserName = UserName.Text,
                Message = entryData.Text,
            });

            entryData.Text = "";

        }
    }
}
