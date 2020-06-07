/***********************************************************************************************
CREATED / DEVELOPED BY: VINCENT LOVELAND
DEPARTMENT: AMC) IT INFRA
DATE: 11/8/2019

NOTES:
(WIRELESS VERSION OF NetConfigBatch)
THIS PROGRAM IS ALSO INTENDED FOR NETWORKS UTILIZING STATIC ADDRESSES
PURPOSE OF THE PROGRAM IS TO GATHER IPV4 ADDRESS, SUBNET AND DEFAULT GATEWAY 
PRIMARY AND ALTERNATE DNS SERVER ADDRESSES ARE ALSO CAPTURED IN THIS PROGRAM
PROGRAM WILL WRITE A SCRIPT FROM OBTAINED NET INFO AND SAVE AS A .BAT FILE TO USER DESKTOP
PROGRAM WILL ALSO DISABLE THE LAN ADPATERS TO ENSURE THE PROPER NETWORK INFORMATION IS CAPTURED
THIS PROGRAM ASSISTS IN THE UPGRADING AND EXCHANGE OF USER PCs
***********************************************************************************************/

using System;
using System.Net;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.IO;

namespace GetUserIp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Simple header for the console
            Console.WriteLine("GETTING USER WIRELESS NET CONFIGURATION AND WRITING BATCH FILE");
            Console.WriteLine("--------------------------------------------------------------");
            Console.WriteLine();

            DisableLAN();
            WirelessNetConfig();
        }

        // Created method for gathering user network configuration settings and writing to batch file
        static void WirelessNetConfig()
        {
            // You can change the last component in parameter to add bat file to different location
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            // Console input for naming the file
            // The string "file" saves .bat to the end of the string so that when it is saved to the location of choice, it creates a functional .bat file
            Console.Write("Enter the name of the batch file you wish to create: ");
            string fileName = Console.ReadLine();
            string file = fileName + ".bat";

            try
            {
                // Deleting file name if name already exists
                if (File.Exists(Path.Combine(docPath, file)))
                {
                    File.Delete(Path.Combine(docPath, file));
                    Console.WriteLine();
                    Console.WriteLine(file + " file name already in use.");
                    Console.WriteLine("Deleting " + file + " and rewriting!");
                }

                // Writing wireless net configuration (IPv4 address, subnet mask, defualt gateway, DNS servers) to batch file
                using (StreamWriter fileWrite = File.CreateText(Path.Combine(docPath, file)))
                {            
                    // Gathering local wireless IPv4 address
                    #region
                    string ipAdd = "";
                    string userIP = "";
                    IPHostEntry host;
                    host = Dns.GetHostEntry(ipAdd);

                    foreach (IPAddress ip in host.AddressList)
                    {
                        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            userIP = ip.ToString();
                        }
                    }
                    #endregion

                    // Gathering subnet mask
                    #region
                    string subnetMask = "";
                    NetworkInterface[] netInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                    foreach (NetworkInterface subMask in netInterfaces)
                    {
                        if (subMask.OperationalStatus == OperationalStatus.Up)
                        {
                            if (subMask.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                            {
                                continue;
                            }

                            UnicastIPAddressInformationCollection UnicastIPInfo = subMask.GetIPProperties().UnicastAddresses;
                            foreach (UnicastIPAddressInformation subnet in UnicastIPInfo.Skip(1))
                            {
                                subnetMask = subnet.IPv4Mask.ToString();
                            }
                        }
                    }
                    #endregion

                    // Gathering the default gateway
                    #region
                    IPAddress defGateway = null;
                    var netAdapt = NetworkInterface.GetAllNetworkInterfaces();
                    if (netAdapt.Any())
                    {
                        foreach (var adapter in netAdapt)
                        {
                            var ipProper = adapter.GetIPProperties();

                            if (ipProper == null)
                            {
                                continue;
                            }

                            var gateway1 = ipProper.GatewayAddresses;

                            if (!gateway1.Any())
                            {
                                continue;
                            }

                            var gateway2 = gateway1.FirstOrDefault(g => g.Address.AddressFamily.ToString() == "InterNetwork");

                            if (gateway2 == null)
                            {
                                continue;
                            }

                            defGateway = gateway2.Address;
                        }
                    }
                    #endregion

                    // Gathering the DNS server information
                    #region
                    string primaryDNS = "";
                    string alternateDNS = "";

                    foreach (NetworkInterface dnsServers in netInterfaces)
                    {
                        if (dnsServers.OperationalStatus == OperationalStatus.Up)
                        {
                            IPInterfaceProperties ipProperties = dnsServers.GetIPProperties();
                            IPAddressCollection dnsAddresses = ipProperties.DnsAddresses;

                            // Returns the first address for DNS servers by using .Take
                            foreach (IPAddress dns in dnsAddresses.Take(1))
                            {
                                primaryDNS = dns.ToString();
                            }

                            // Returns the second address for DNS servers by using .Skip(1)
                            foreach (IPAddress dns in dnsAddresses.Skip(1))
                            {
                                alternateDNS = dns.ToString();
                            }
                            break;
                        }
                    }
                    #endregion

                    // Gathering the network adapter name to be included into the batch file
                    #region
                    string adapterName = "";
                    foreach (NetworkInterface netAdapter in netInterfaces)
                    {
                        // If check for getting just the currently connected adapter as well as excluding the tunnel and loopback adapters
                        if (netAdapter.OperationalStatus == OperationalStatus.Up && netAdapter.NetworkInterfaceType != NetworkInterfaceType.Tunnel && netAdapter.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                        {
                            adapterName = netAdapter.Name;
                        }
                    }
                    #endregion

                    // Writing the batch file script
                    fileWrite.WriteLine("@echo off");
                    fileWrite.WriteLine("netsh interface ipv4 set address name=\""+ adapterName +"\" static " + userIP + " " + subnetMask + " " + defGateway);
                    fileWrite.WriteLine();
                    fileWrite.WriteLine("@echo off");
                    fileWrite.WriteLine("netsh interface ipv4 set dns name=\"" + adapterName + "\" static " + primaryDNS + ""); 
                    fileWrite.WriteLine("netsh interface ipv4 add dns name=\"" + adapterName + "\" " + alternateDNS + " index=2"); 
                }

                // Final console output
                Console.WriteLine();
                Console.WriteLine(file + " has successfully been created and added to user's desktop!");
                Console.WriteLine();
                Console.WriteLine("Press ENTER to exit....");
                Console.ReadLine();
            }
            catch (Exception MyExcep)
            {
                Console.WriteLine(MyExcep.ToString());
            }
        }

        // Disable LAN adapters. If on and connected, program will can capture incorrect network configuration
        // Found that this is sometimes not needed, but is more of a just in case function to ensure proper information is captured
        static void DisableLAN()
        {
            // Due to manufacturers having different adapter names, common adapter names are used when disabling the LAN adapters
            // These are two common names among the computers that are found at Hankook Tire TN Plant

            Process disable1 = new Process();
            {
                var lanAdap1 = ("Ethernet");
                ProcessStartInfo startInfo = new ProcessStartInfo("netsh", "interface set interface \"" + lanAdap1 + "\" disable");
                disable1.StartInfo = startInfo;
                disable1.Start();
            }

            Process disable2 = new Process();
            {
                var lanAdap2 = ("Local Area Connection");
                ProcessStartInfo startInfo = new ProcessStartInfo("netsh", "interface set interface \"" + lanAdap2 + "\" disable");
                disable2.StartInfo = startInfo;
                disable2.Start();
            }
        }
    }
}
