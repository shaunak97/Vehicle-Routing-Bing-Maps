using BingMapsRESTToolkit;
using BingMapsRESTToolkit.Extensions;
using Microsoft.Maps.MapControl.WPF;
using Microsoft.Maps.MapControl.WPF.Overlays;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TravellingSalesmenRouteSample
{
    public partial class MainWindow : Window
    {
        #region Private Properties

        private string BingMapsKey = System.Configuration.ConfigurationManager.AppSettings.Get("BingMapsKey");

        private string SessionKey;

        private Regex CoordinateRx = new Regex(@"^[\s\r\n\t]*(-?[0-9]{0,2}(\.[0-9]*)?)[\s\t]*,[\s\t]*(-?[0-9]{0,3}(\.[0-9]*)?)[\s\r\n\t]*$");

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            MyMap.CredentialsProvider = new ApplicationIdCredentialsProvider(BingMapsKey);
            MyMap.CredentialsProvider.GetCredentials((c) =>
            {
                SessionKey = c.ApplicationId;
            });

            //Add some sample locations to the input panel.
            InputTbx.Text = "Seattle, WA\r\nRedmond, WA\r\nBellevue, WA\r\nEverett, WA\r\nTacoma, WA\r\nKirkland, WA\r\nSammamish, WA\r\nLynnwood, WA\r\nRenton, WA\r\nDuvall, WA\r\nMonroe, WA\r\nSumner, WA";
        }

        /*
        private async void DistanceMatrixBtn_Clicked(object sender, RoutedEventArgs e)
        {
            MyMap.Children.Clear();
            OutputTbx.Text = string.Empty;
            LoadingBar.Visibility = Visibility.Visible;

            var r = new DistanceMatrixRequest()
            {
                Origins = new List<SimpleWaypoint>()
                {
                    new SimpleWaypoint(47.6044, -122.3345),
                    new SimpleWaypoint(47.6731, -122.1185),
                    new SimpleWaypoint(47.6149, -122.1936)
                },
                Destinations = new List<SimpleWaypoint>()
                {
                    new SimpleWaypoint(45.5347, -122.6231),
                    new SimpleWaypoint(47.4747, -122.2057),
                },
                BingMapsKey = BingMapsKey,
                TimeUnits = TimeUnitType.Minute,
                DistanceUnits = DistanceUnitType.Miles,
                TravelMode = TravelModeType.Driving
            };

            var ans = await r.Execute();
           // RenderRouteResponse(r, ans);

        }
        */
          private async void CalculateRouteBtn_Clicked(object sender, RoutedEventArgs e)
          {
              MyMap.Children.Clear();
              OutputTbx.Text = string.Empty;
              LoadingBar.Visibility = Visibility.Visible;

              var waypoints = GetWaypoints();

              if (waypoints.Count < 2)
              {
                  MessageBox.Show("Need a minimum of 2 waypoints to calculate a route.");
                  return;
              }

              var travelMode = (TravelModeType)Enum.Parse(typeof(TravelModeType), (string)(TravelModeTypeCbx.SelectedItem as ComboBoxItem).Content);
              var tspOptimization = (TspOptimizationType)Enum.Parse(typeof(TspOptimizationType), (string)(TspOptimizationTypeCbx.SelectedItem as ComboBoxItem).Tag);
              try
              {
                  //Calculate a route between the waypoints so we can draw the path on the map. 
                  var routeRequest = new RouteRequest()
                  {
                      Waypoints = waypoints,

                      //Specify that we want the route to be optimized.
                      WaypointOptimization = tspOptimization,


                      RouteOptions = new RouteOptions()
                      {
                          TravelMode = travelMode,


                          RouteAttributes = new List<RouteAttributeType>()
                          {
                              RouteAttributeType.RoutePath,
                         ///     RouteAttributeType.ExcludeItinerary
                          }
                      },

                      //When straight line distances are used, the distance matrix API is not used, so a session key can be used.
                      BingMapsKey = (tspOptimization == TspOptimizationType.StraightLineDistance)? SessionKey : BingMapsKey
                  };

                  

                  //Input start Time for Travel time with Traffic

                  DateTime dtd = new DateTime(2020,5,29, 23, 00, 00);
                 // string dtd = "18:00:00";
                  routeRequest.RouteOptions.DateTime=dtd;

                  routeRequest.RouteOptions.DistanceUnits = DistanceUnitType.Miles;


                  //Only use traffic based routing when travel mode is driving.
                  if(routeRequest.RouteOptions.TravelMode != TravelModeType.Driving)
                  {
                      routeRequest.RouteOptions.Optimize = RouteOptimizationType.Time;
                  }
                  else
                  {
                      routeRequest.RouteOptions.Optimize = RouteOptimizationType.TimeWithTraffic;
                  }

                  var r = await routeRequest.Execute();

                  RenderRouteResponse(routeRequest, r);                
              }
              catch (Exception ex)
              {
                  MessageBox.Show("Error: " + ex.Message);
              }

              LoadingBar.Visibility = Visibility.Collapsed;
          }

          


        #region Private Methods



        /// <summary>
        /// Renders a route response on the map.
        /// </summary>
        private void RenderRouteResponse(RouteRequest routeRequest, Response response)
        {
            //Render the route on the map.
            if (response != null && response.ResourceSets != null && response.ResourceSets.Length > 0 &&
               response.ResourceSets[0].Resources != null && response.ResourceSets[0].Resources.Length > 0
               && response.ResourceSets[0].Resources[0] is Route)
            {
                var route = response.ResourceSets[0].Resources[0] as Route;

                var timeSpan = new TimeSpan(0, 0, (int)Math.Round(route.TravelDurationTraffic));

                if (timeSpan.Days > 0)
                {
                    OutputTbx.Text = string.Format("Travel Time: {3} days {0} hr {1} min {2} sec\r\n", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Days);
                    OutputTbx.AppendText("\n");
                }
                else
                {
                    OutputTbx.Text = string.Format("Travel Time: {0} hr {1} min {2} sec\r\n", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                    OutputTbx.AppendText("\n");
                }
               
                ///Hard code start date and time
                DateTime dt = new DateTime(2020, 5, 29, 23, 0, 0);

                /*      
                      var tspan = route.RouteLegs[0].TravelDuration;
                      TimeSpan tispan = new TimeSpan(0,0,(int)(tspan));
                      DateTime newDate = dt.Add(tispan);
                      string ndt = (newDate).ToString();
                      OutputTbx.AppendText(ndt);
                */

             //  var dest = DMatrix.Destination;
                
                

                //Route Travel Distance (In Miles)
                OutputTbx.AppendText("Travel Distance: ");
                var dist = route.TravelDistance;
                var dis = dist.ToString();
                OutputTbx.AppendText(dis);
                OutputTbx.AppendText(" mi\n\n");

                ///Start Location and Start Time Output
                string num = (1).ToString();
                OutputTbx.AppendText(num); OutputTbx.AppendText("--");
                OutputTbx.AppendText(routeRequest.Waypoints[0].Address);
                OutputTbx.AppendText("\t");
                string ndt = (dt).ToString();
                OutputTbx.AppendText(ndt);
                OutputTbx.AppendText("\n");

///Output - Ordered Waypoints and corresponding ETA
                for (var i = 1; i < routeRequest.Waypoints.Count; i++)
                {
                     num = (i + 1).ToString();
                    OutputTbx.AppendText(num); OutputTbx.AppendText("--");
                    OutputTbx.AppendText(routeRequest.Waypoints[i].Address);
                    OutputTbx.AppendText("\t");
                    var tspan = route.RouteLegs[i-1].TravelDuration;
                    TimeSpan tispan = new TimeSpan(0, 0, (int)(tspan));
                    DateTime newDate = dt.Add(tispan); dt = newDate;
                     ndt = (newDate).ToString();
                    OutputTbx.AppendText(ndt);
                    OutputTbx.AppendText("\n");
                }


                ///for (var i = 0; i < routeRequest.Waypoints.Count; i++)
                /*    for (var i = 0; i < routeRequest.Waypoints.Count-1; i++)
                    {
                        var tspan = route.RouteLegs[i].TravelDuration;
                        string tnum = (tspan).ToString();
                        OutputTbx.AppendText(tnum);
                        OutputTbx.AppendText("\n");
                    }
                */

                var routeLine = route.RoutePath.Line.Coordinates;
                var routePath = new LocationCollection();

                for (int i = 0; i < routeLine.Length; i++)
                {
                    routePath.Add(new Microsoft.Maps.MapControl.WPF.Location(routeLine[i][0], routeLine[i][1]));
                }

                var routePolyline = new MapPolyline()
                {
                    Locations = routePath,
                    Stroke = new SolidColorBrush(Colors.Red),
                    StrokeThickness = 3
                };

                MyMap.Children.Add(routePolyline);

                var locs = new List<Microsoft.Maps.MapControl.WPF.Location>();

                //Create pushpins for the optimized waypoints.
                //The waypoints in the request were optimized for us.
                for (var i = 0; i < routeRequest.Waypoints.Count; i++)
                {
                    var loc = new Microsoft.Maps.MapControl.WPF.Location(routeRequest.Waypoints[i].Coordinate.Latitude, routeRequest.Waypoints[i].Coordinate.Longitude);
                    
                    //Only render the last waypoint when it is not a round trip.
                    if (i < routeRequest.Waypoints.Count - 1)
                    {
                        MyMap.Children.Add(new Pushpin()
                        {
                            Location = loc,
                            Content = i
                        });
                    }
                   
                    locs.Add(loc);
                }

                MyMap.SetView(locs, new Thickness(50), 0);
            }
            else if (response != null && response.ErrorDetails != null && response.ErrorDetails.Length > 0)
            {
                throw new Exception(String.Join("", response.ErrorDetails));
            }
            


        }

        /// <summary>
        /// Gets the inputted waypoints.
        /// </summary>
        private List<SimpleWaypoint> GetWaypoints()
        {
            var places = InputTbx.Text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var waypoints = new List<SimpleWaypoint>();

            foreach (var p in places)
            {
                if (!string.IsNullOrWhiteSpace(p))
                {
                    var m = CoordinateRx.Match(p);

                    if (m.Success)
                    {
                        waypoints.Add(new SimpleWaypoint(double.Parse(m.Groups[1].Value), double.Parse(m.Groups[3].Value)));
                    }
                    else
                    {
                        waypoints.Add(new SimpleWaypoint(p));
                    }
                }
            }

            return waypoints;
        }

        #endregion
    }
}
