using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace VehicleEventHandling
{
    public class Program
    {
        public static event EventHandler<FieldEventArgs<Journey, Vehicle>> CurrentVehicleChanged;

        static void Main(string[] args)
        {
            var journey = new Journey("J1");
            var journey2 = new Journey("J2");
            var vehicle1 = new Vehicle { Name = "V1" };
            var vehicle2 = new Vehicle { Name = "V2" };
            var handler = new VehicleHandler();

            //TODO: ---- Replace this:
            handler.Start();
            CurrentVehicleChanged += handler.OnJourneyCurrentVehicleChanged;
            // ----

            /* output:
               J1 CurrentVehicleChanged from null to V1
               J2 CurrentVehicleChanged from null to V2
               Handling vehicle: V1 assigned to journey: J1
               Handling vehicle: V2 assigned to journey: J2
               Handling vehicle: V1 assigned to journey: J1
               Handling vehicle: V2 assigned to journey: J2
               Handling vehicle: V1 assigned to journey: J1
               Handling vehicle: V2 assigned to journey: J2
               J1 CurrentVehicleChanged from V1 to null       <--- J1 doesn't have vehicle assigned and is not handled for next 3 seconds
               Handling vehicle: V2 assigned to journey: J2
               Handling vehicle: V2 assigned to journey: J2
               Handling vehicle: V2 assigned to journey: J2
               J1 CurrentVehicleChanged from null to V2
               J2 CurrentVehicleChanged from V2 to V1
               Handling vehicle: V1 assigned to journey: J2
               Handling vehicle: V2 assigned to journey: J1
               Handling vehicle: V1 assigned to journey: J2
               Handling vehicle: V2 assigned to journey: J1
               Handling vehicle: V1 assigned to journey: J2
               Handling vehicle: V2 assigned to journey: J1
               J1 CurrentVehicleChanged from V2 to null      <--- J1 doesn't have vehicle assigned and is not handled anymore
               Handling vehicle: V1 assigned to journey: J2
               Handling vehicle: V1 assigned to journey: J2
               Handling vehicle: V1 assigned to journey: J2
               Handling vehicle: V1 assigned to journey: J2
               ...
             */


            //TODO: ---- with this, problem is that this don't stop the observable.interval when the journey.ChangeVehicle(null);"
            //var observable = Observable.FromEventPattern<FieldEventArgs<Journey, Vehicle>>(
            //        h => CurrentVehicleChanged += h,
            //        h => CurrentVehicleChanged -= h)
            //    .Do(x => Console.WriteLine($"{x.EventArgs.Item.Name} CurrentVehicleChanged from {x.EventArgs.LastValue?.Name ?? "null"} to {x.EventArgs.Value?.Name ?? "null"}"))
            //    .Select(x => x.EventArgs.Item)
            //    .Where(VehicleHandler.Validate)
            //    .Select(x => x.CurrentVehicle)
            //    .Distinct()
            //    .SelectMany(x => Observable.Interval(TimeSpan.FromSeconds(2)).Select(_ => x))
            //    .Subscribe(x => Console.WriteLine($"Handling vehicle: {x.Name} assigned to journey: {x.CurrentJourneyName}"));
            // ----


            /* output:
               J1 CurrentVehicleChanged from null to V1
               J2 CurrentVehicleChanged from null to V2
               Handling vehicle: V1 assigned to journey: J1
               Handling vehicle: V2 assigned to journey: J2
               Handling vehicle: V1 assigned to journey: J1
               Handling vehicle: V2 assigned to journey: J2
               J1 CurrentVehicleChanged from V1 to null
               Handling vehicle: V2 assigned to journey: J2
               Handling vehicle: V1 assigned to journey: J1 <-- still handled after J1.CurrentVehicle set to null
               Handling vehicle: V1 assigned to journey: J1
               Handling vehicle: V2 assigned to journey: J2
               Handling vehicle: V2 assigned to journey: J2
               Handling vehicle: V1 assigned to journey: J1
               J1 CurrentVehicleChanged from null to V2
               Handling vehicle: V2 assigned to journey: J1
               J2 CurrentVehicleChanged from V2 to V1
               Handling vehicle: V1 assigned to journey: J2
               Handling vehicle: V1 assigned to journey: J2
               Handling vehicle: V2 assigned to journey: J1
               Handling vehicle: V1 assigned to journey: J2
               Handling vehicle: V2 assigned to journey: J1
               Handling vehicle: V2 assigned to journey: J1
               Handling vehicle: V1 assigned to journey: J2
               J1 CurrentVehicleChanged from V2 to null
               Handling vehicle: V1 assigned to journey: J2
               Handling vehicle: V2 assigned to journey: J1 <-- still handled after J1.CurrentVehicle set to null
               Handling vehicle: V2 assigned to journey: J1
               Handling vehicle: V1 assigned to journey: J2
               Handling vehicle: V2 assigned to journey: J1
               Handling vehicle: V1 assigned to journey: J2
               Handling vehicle: V2 assigned to journey: J1
               Handling vehicle: V1 assigned to journey: J2
               Handling vehicle: V2 assigned to journey: J1            
             */


            // Simulate changes
            journey.ChangeVehicle(vehicle1);
            journey2.ChangeVehicle(vehicle2);
            Thread.Sleep(TimeSpan.FromSeconds(6));

            journey.ChangeVehicle(null);
            Thread.Sleep(TimeSpan.FromSeconds(6));

            journey.ChangeVehicle(vehicle2);
            journey2.ChangeVehicle(vehicle1);
            Thread.Sleep(TimeSpan.FromSeconds(6));

            journey.ChangeVehicle(null);

            Console.ReadLine();
        }

        public class VehicleHandler
        {
            private readonly HashSet<Vehicle> _vehiclesToHandle = new();
            readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(2);


            public void OnJourneyCurrentVehicleChanged(object sender, FieldEventArgs<Journey, Vehicle> e)
            {
                var journey = e.Item;
                if (Validate(journey))
                {
                    _vehiclesToHandle.Add(journey.CurrentVehicle);
                }
                else if (journey.CurrentVehicle == null && e.LastValue != null)
                {
                    _vehiclesToHandle.Remove(e.LastValue);
                }
                Console.WriteLine($"{e.Item.Name} CurrentVehicleChanged from {e.LastValue?.Name ?? "null"} to {e.Value?.Name ?? "null"}");
            }

            public static bool Validate(Journey journey)
            {
                return journey.CurrentVehicle != null;
            }

            public void Start()
            {
                Observable.Interval(_updateInterval)
                    .SelectMany(x => _vehiclesToHandle)
                    .Subscribe(x =>
                    {
                        Console.WriteLine($"Handling vehicle: {x.Name} assigned to journey: {x.CurrentJourneyName}");
                    });
            }
        }


        public class Vehicle
        {
            public string Name { get; set; }
            public string CurrentJourneyName { get; set; }
        }

        public class Journey
        {
            public Journey(string name)
            {
                Name = name;
            }

            public string Name { get; set; }

            private Vehicle _currentVehicle;


            public Vehicle CurrentVehicle
            {
                get => _currentVehicle;
                private set
                {
                    var lastValue = _currentVehicle;

                    _currentVehicle = value;
                    if (_currentVehicle != null) _currentVehicle.CurrentJourneyName = this.Name;

                    CurrentVehicleChanged?.Invoke(this, new FieldEventArgs<Journey, Vehicle>(this, _currentVehicle, lastValue));
                }
            }

            public void ChangeVehicle(Vehicle vehicle)
            {
                CurrentVehicle = vehicle;
            }
        }

        public class FieldEventArgs<TItem, TValue> : EventArgs
        {
            public TItem Item { get; }
            public TValue Value { get; }
            public TValue LastValue { get; }

            public FieldEventArgs(TItem item, TValue value, TValue lastValue)
            {
                Item = item;
                Value = value;
                LastValue = lastValue;
            }
        }

    }
}
