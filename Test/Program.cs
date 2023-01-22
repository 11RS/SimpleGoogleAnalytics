using GoogleAnalytics;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        private const int Sessions = 2;
        private const int Pages = 2;
        private const int Events = 2;

        static async Task Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage Test <Google Analytics Measurement Id> <Api Secret> <clientId>");
                return;
            }

            Console.WriteLine("Please adjust launchSettings.json for you own needs!{0} Close this window if not yet done. {1} Press return to continue",Environment.NewLine, Environment.NewLine);
            Console.ReadLine();


            string trackingId = args[0];
            string apiSecret = args[1];
            string clientId = args[2];

            var analytics = new Analytics()
            {
                MeasurementId = trackingId,
                ApiSecret = apiSecret,
                ClientId = clientId
            };

            for (var iSession = 1; iSession <= Sessions; iSession++)
            {
                //Todo RoS:
                //It seems we somehow need to:
                //-generate ga_session_id and ga_session_number
                //-and pass with all events
                var sessionId = Guid.NewGuid().ToString();

                var s = new SessionStartMeasurement()
                {
                    SessionId = sessionId,
                    SessionNumber = iSession.ToString(),
                };

                await ProcessMeasurement(analytics, s, s);

                for (var pageNr = 1; pageNr <= Pages; pageNr++)
                {
                    await EmulatePageInteractions(analytics, s, pageNr);
                }
            }
        }

        private static async Task EmulatePageInteractions(Analytics analytics, SessionStartMeasurement sessionStart, int pageNr)
        {
            var pv = new PageMeasurement()
            {
                Path = $"Page {pageNr}",
                Title = $"Page {pageNr}",
                HostName = "www.test99.ch",
                UserAgent = "Target"
            };
            await ProcessMeasurement(analytics, sessionStart, pv);

            for (var eventNr = 1; eventNr <= Events; eventNr++)
            {
                var m = new TestEventMeasurement()
                {
                    Action = $"Action {pageNr}.{eventNr}",
                    TestTime = DateTime.Now.Ticks,
                    Result = "passed",
                    Bugs = 0,
                };
                await ProcessMeasurement(analytics,sessionStart , m);
            }
        }

        private static async Task ProcessMeasurement(Analytics analytics, SessionStartMeasurement sessionStart, Measurement s)
        {
            //Todo RoS: ValidateMeasurements failed posting more than one event
            //So we send them one by one for now
            analytics.Events.Add(s);

            //Todo RoS: doesn't work - validation fails
            //foreach(var ev in analytics.Events)
            //{
            //    ev.SessionId = sessionStart.SessionId;
            //    ev.SessionNumber = sessionStart.SessionNumber;
            //}

            await ProcessMeasurement(analytics);
            analytics.Events.Clear();
        }

        private static async Task ProcessMeasurement(Analytics analytics)
        {
            var errors = await HttpProtocol.ValidateMeasurements(analytics);
            if (errors.ValidationMessages?.Length > 0)
            {
                foreach (var error in errors.ValidationMessages)
                {
                    Console.WriteLine("{0}: {1}", error.ValidationCode, error.Description);
                }
            }
            else
            {
                await HttpProtocol.PostMeasurements(analytics);
                Console.WriteLine("measurement sent!!");
            }
        }
    }
}
