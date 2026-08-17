using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace FreightSystem
{
    //REQUIREMENT 3: CUSTOM EXCEPTION
    //We create our own error type for when someone tries to ship to the same location.
    public class InvalidRouteException : Exception
    {
        public InvalidRouteException(string message) : base(message) { }
    }

    //REQUIREMENT 2: INTERFACES (At least two)
    public interface ITrackable
    {
        string TrackingId { get; }
    }

    public interface IRoutable
    {
        string Origin { get; }
        string Destination { get; }
    }

    //REQUIREMENT 1: ABSTRACTION & ENCAPSULATION
    //Abstract class means we can't create a generic "Shipment", only specific types.
    public abstract class Shipment : ITrackable, IRoutable
    {
        //Encapsulation: Hiding data using properties
        public string TrackingId { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public int TransitSeconds { get; set; } //Time it takes to deliver

        public Shipment(string origin, string destination)
        {
            TrackingId = "TRK-" + new Random().Next(1000, 9999);
            Origin = origin;
            Destination = destination;

            //Throwing our custom exception if the origin and destination are the same
            if (origin == destination)
            {
                throw new InvalidRouteException($"Error: Cannot route from {origin} to {destination}. It's the same place!");
            }
        }

        //Abstract method that forces child classes to create their own version
        public abstract void CalculateTransitTime();
    }

    //REQUIREMENT 1: INHERITANCE & POLYMORPHISM
    //StandardParcel inherits from Shipment
    public class StandardParcel : Shipment
    {
        public StandardParcel(string origin, string dest) : base(origin, dest) { }

        //Polymorphism: Overriding the transit time specifically for Standard parcels
        public override void CalculateTransitTime()
        {
            TransitSeconds = 10; //Takes 10 seconds to deliver
        }
    }

    //PriorityFreight also inherits from Shipment
    public class PriorityFreight : Shipment
    {
        public PriorityFreight(string origin, string dest) : base(origin, dest) { }

        //Polymorphism
        public override void CalculateTransitTime()
        {
            TransitSeconds = 4; //Takes only 4 seconds to deliver
        }
    }

    //REQUIREMENT 4: EVENTS & DELEGATES
    public class FreightManager
    {
        //Define the Delegate
        public delegate void SystemEventHandler(string message);

        //Define the Events
        public event SystemEventHandler OnDispatched;
        public event SystemEventHandler OnDelivered;

        //Lists to store our shipments
        public List<Shipment> ActiveShipments = new List<Shipment>();
        public List<Shipment> DeliveredShipments = new List<Shipment>();

        public void Dispatch(Shipment shipment)
        {
            shipment.CalculateTransitTime();
            ActiveShipments.Add(shipment);

            //Trigger the Dispatched Event
            if (OnDispatched != null)
            {
                OnDispatched($"[DISPATCHED] {shipment.TrackingId} is moving from {shipment.Origin} to {shipment.Destination}.");
            }
        }

        public void CompleteDelivery(Shipment shipment)
        {
            DeliveredShipments.Add(shipment);

            //Trigger the Delivered Event
            if (OnDelivered != null)
            {
                OnDelivered($"[DELIVERED] {shipment.TrackingId} has arrived safely!");
            }
        }
    }

    class Program
    {
        static FreightManager manager = new FreightManager();
        static bool systemRunning = true;

        static void Main()
        {
            //Subscribe to the events
            manager.OnDispatched += PrintMessage;
            manager.OnDelivered += PrintMessage;

            //REQUIREMENT 5: THREADING & MULTITHREADING
            //Start a background process that runs independently of the user menu
            Thread backgroundWorker = new Thread(MonitorShipments);
            backgroundWorker.IsBackground = true;
            backgroundWorker.Start();

            //The main User Menu loop
            while (systemRunning)
            {
                Console.WriteLine("\n--- SMART FREIGHT CONSOLE ---");
                Console.WriteLine("1. Add Standard Parcel");
                Console.WriteLine("2. Add Priority Freight");
                Console.WriteLine("3. View Active Shipments");
                Console.WriteLine("4. Force Error (Ship to same location)");
                Console.WriteLine("5. Exit");
                Console.Write("Choice: ");

                string choice = Console.ReadLine();

                //REQUIREMENT 3: TRY-CATCH-FINALLY EXCEPTION HANDLING
                try
                {
                    switch (choice)
                    {
                        case "1":
                            manager.Dispatch(new StandardParcel("JHB", "PTA"));
                            break;
                        case "2":
                            manager.Dispatch(new PriorityFreight("CPT", "JHB"));
                            break;
                        case "3":
                            Console.WriteLine($"\nThere are {manager.ActiveShipments.Count} active shipments.");
                            break;
                        case "4":
                            //This will purposely trigger our custom exception
                            manager.Dispatch(new StandardParcel("PTA", "PTA"));
                            break;
                        case "5":
                            systemRunning = false;
                            Console.WriteLine("Shutting down...");
                            break;
                        default:
                            Console.WriteLine("Invalid option.");
                            break;
                    }
                }
                catch (InvalidRouteException ex) //Catching our custom exception
                {
                    Console.WriteLine($"\n[SYSTEM HALT] {ex.Message}");
                }
                catch (Exception ex) //Catching any other general errors
                {
                    Console.WriteLine($"\n[ERROR] Something went wrong: {ex.Message}");
                }
                finally
                {
                    //Finally always runs, good for simple logging or continuing
                    Console.WriteLine("--- Transaction Attempted ---");
                }
            }
        }

        //REQUIREMENT 5: BACKGROUND THREAD LOGIC
        static void MonitorShipments()
        {
            while (systemRunning)
            {
                Thread.Sleep(1000); //Wait 1 second

                //We loop backwards through the list to safely remove items while checking them
                for (int i = manager.ActiveShipments.Count - 1; i >= 0; i--)
                {
                    Shipment current = manager.ActiveShipments[i];
                    current.TransitSeconds--; //Reduce time by 1 second

                    if (current.TransitSeconds <= 0)
                    {
                        //It has arrived! Remove from active, and complete delivery.
                        manager.ActiveShipments.RemoveAt(i);
                        manager.CompleteDelivery(current);

                        //REQUIREMENT 6: BONUS FEATURE (FILE I/O)
                        //Saves a permanent record to a text file
                        File.AppendAllText("DeliveryLog.txt", $"{DateTime.Now}: {current.TrackingId} Delivered.\n");
                    }
                }
            }
        }

        //This method is called whenever an event is triggered
        static void PrintMessage(string message)
        {
            Console.WriteLine($"\n*** EVENT ALERT: {message} ***");
        }
    }
}
