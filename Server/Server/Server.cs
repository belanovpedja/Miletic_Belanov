using PomocneKlase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;


namespace Server
{
    internal class Server
    {

        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();
            string a;
            Dictionary<int, Parking> recnikParkinga = new Dictionary<int, Parking>();
            Dictionary<string, Zauzece> recnik_zauzeca = new Dictionary<string, Zauzece>();
            List<Socket> acceptedSockets = new List<Socket>();


            #region UnosParkinga


            do
            {
                Console.WriteLine("Unesi broj parkinga za koji cete uneti podatke: ");
                int brP = int.Parse(Console.ReadLine());

                Console.WriteLine("Unesi ukupan broj mesta: ");
                int brM = int.Parse(Console.ReadLine());

                Console.WriteLine("Unesi samo broj zauzetih mesta: ");
                int brZ = int.Parse(Console.ReadLine());

                Console.WriteLine("Unesi cenu po satu: ");
                double c = int.Parse(Console.ReadLine());

                Parking parking = new Parking(brM, brZ, c, 0);
                recnikParkinga.Add(brP, parking);

                Console.WriteLine($"\nUneli ste parking sa brojem: {brP}. \n");

                Console.WriteLine("Zelis li da uneses sledeci parking?\n DA/NE \n");
                a = (Console.ReadLine());
            }
            while (a.ToLower() == "da");
            #endregion


            #region Inicijalzacija Komunikacije

            Socket UdpServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint ServerEP = new IPEndPoint(IPAddress.Loopback, 15000);
            UdpServerSocket.Bind(ServerEP);
            UdpServerSocket.Blocking = false;

            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 20000);
            serverSocket.Bind(serverEP);
            serverSocket.Blocking = false;
            serverSocket.Listen(100);
            Console.WriteLine($"Server parkinga je pokrenut!");
            #endregion




            while (true)
            {
                if (UdpServerSocket.Poll(2000 * 1000, SelectMode.SelectRead))
                {
                    #region  UDP 
                    try
                    {
                        EndPoint clientEndPoint = new IPEndPoint(IPAddress.None, 0);
                        byte[] recBuffer = new byte[1024];

                        int bytesReceived = UdpServerSocket.ReceiveFrom(recBuffer, ref clientEndPoint);

                        string zahtev = Encoding.UTF8.GetString(recBuffer);
                        Console.WriteLine($"{zahtev}");

                        string hostName = Dns.GetHostName();
                        IPAddress[] addresses = Dns.GetHostAddresses(hostName);
                        IPAddress selectedAddress = null;

                        foreach (var address in addresses)
                        {
                            if (address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                selectedAddress = address;
                                break;
                            }
                        }

                        if (selectedAddress == null)
                        {
                            Console.WriteLine("IPv4 adresa nije pronađena. Proverite mrežne postavke.");
                            return;
                        }
                        // A moze i selectedAddress=loopback ?? ili koju vec adresu hocu za tcp tj adresu racunara

                        int port = 20000;
                        string TCPpodaci = ($"{selectedAddress} {port}");


                        byte[] TCPpodaciUBajtima = Encoding.UTF8.GetBytes(TCPpodaci);
                        UdpServerSocket.SendTo(TCPpodaciUBajtima, clientEndPoint);

                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine($"Socket greška: {ex.Message}");
                    }
                    #endregion
                }

                #region TCP

                if (serverSocket.Poll(2000 * 1000, SelectMode.SelectRead))
                {
                    Socket acceptedSocket = serverSocket.Accept();
                    IPEndPoint clientEP = acceptedSocket.RemoteEndPoint as IPEndPoint;
                    if (acceptedSockets.Contains(acceptedSocket) == false)
                    {
                        acceptedSockets.Add(acceptedSocket);

                        string parkingInfo=string.Empty;
                        foreach (var pair in recnikParkinga)
                        {
                            parkingInfo = ($"{parkingInfo} Parking sa brojem {pair.Key} ima:  " +
                                                  $"{pair.Value.BrojZauzetih}/{pair.Value.BrojMesta} (zauzeta mesta/ukupno mesta)\n");

                            
                        }
                        acceptedSocket.Send(Encoding.UTF8.GetBytes(parkingInfo));

                    }

                }
                if (acceptedSockets.Count < 1)
                {
                    if (stopwatch.Elapsed.TotalSeconds == 0)
                    {
                        stopwatch.Start();

                    }
                    else if (stopwatch.Elapsed.TotalSeconds >= 20)
                    {
                        Console.WriteLine("Nema nikoga vise od 20s, unesi 'kraj' za gasenje Dispecera.");
                        if (Console.ReadLine() == "kraj")
                        {
                            foreach (var x in recnikParkinga)
                            {
                                Console.WriteLine($"Zarada dispecera na parkingu br. {x.Key} je {x.Value.Zarada} din.");
                            }

                            Console.WriteLine("Server zavrsava sa radom");
                            UdpServerSocket.Close();
                            serverSocket.Close();
                            Console.ReadKey();
                            break;
                        }
                    }
                    

                    Console.WriteLine("Cekam auto...");

                    continue;
                }
                else stopwatch.Restart();

                foreach (Socket acceptedSocket in acceptedSockets)
                {
                    acceptedSocket.Blocking = false;
                }







                //mislim da ide bez jos jednog while-a ovde jer nije igra sa 2 igraca da se konektuju, nego uvek proverava dal je doso novi auto
                try
                {
                    for (int i = 0; i < acceptedSockets.Count; i++)
                    {
                        if (acceptedSockets[i].Poll(1500 * 1000, SelectMode.SelectRead))
                        {



                            #region IZLAZ SA PARKINGA 

                            byte[] buffer = new byte[4096];
                            int brBajta = acceptedSockets[i].Receive(buffer);
                            if (brBajta == 0)
                                break;
                            string izlazniID = Encoding.UTF8.GetString(buffer);

                            Match match = Regex.Match(izlazniID, @"\[(\d+)\]");

                            if (match.Success) //AKO NE RADI OVAKO regex MOGU SA StartsWith [
                            {
                                izlazniID = match.Groups[1].Value;


                                //brise ID ako postoji  i update podataka u parkingu + racun

                                if (recnik_zauzeca.ContainsKey(izlazniID) == true)
                                {
                                    double cena = 0;
                                    //racunam racun i saljem klijentu da on potvrdi
                                    recnik_zauzeca.TryGetValue(izlazniID, out Zauzece zauzece);

                                    string[] vreme = zauzece.VremeNapustanja.Split(':');
                                    int satKlijenta = int.Parse(vreme[0]);
                                    int minutKlijenta = int.Parse(vreme[1]);
                                    int ukMinuta = (satKlijenta - zauzece.VremeDolaska[0]) * 60 + (minutKlijenta - zauzece.VremeDolaska[1]);

                                    int zapocetihSati = 0;
                                    if (ukMinuta % 60 != 0)
                                        zapocetihSati = 1;
                                    zapocetihSati += ukMinuta / 60;


                                    foreach (var x in recnikParkinga)
                                    {
                                        if (x.Key == zauzece.BrParkinga)
                                        {
                                            cena = (x.Value.Cena) * (zauzece.BrMesta) * (zapocetihSati);
                                            break;
                                        }

                                    }




                                    //ispis auto koji napustaju parking
                                    Console.WriteLine("Automobili koji napustaju parking:\n");
                                    foreach (var x in zauzece.AutoInfo)
                                    {
                                        if (x == string.Empty)
                                            break;
                                        Console.WriteLine($"{x} \n");
                                    }

                                    foreach (var x in recnikParkinga)
                                    {
                                        if (x.Key == zauzece.BrParkinga)
                                        {
                                            x.Value.Zarada += cena;

                                            x.Value.BrojZauzetih -= zauzece.BrMesta;
                                            Console.WriteLine($"Na parkingu broj {x.Key} sada ima {x.Value.BrojZauzetih} od {x.Value.BrojMesta} mesta");
                                            break;
                                        }
                                    }

                                    recnik_zauzeca.Remove(izlazniID);

                                    byte[] ok = new byte[1024];
                                    acceptedSockets[i].Send(Encoding.UTF8.GetBytes($"CENA: {cena} din. unesi OK ako potvrdjujes izlaz. "));

                                    #region Zatvaranje Soketa


                                    acceptedSockets.Remove(acceptedSockets[i]);
                                    acceptedSockets[i].Close();
                                    Console.ReadKey();

                                    #endregion

                                }
                                else
                                {
                                    Console.WriteLine("Nevalidan ID unesen od klijenta");
                                }


                                #endregion
                            }


                            else
                            {
                                BinaryFormatter formatter = new BinaryFormatter();



                                using (MemoryStream ms = new MemoryStream(buffer, 0, brBajta))
                                {
                                    Zauzece zauzece = (Zauzece)formatter.Deserialize(ms);

                                    Console.WriteLine($"Originalan zahtev -> Parking: {zauzece.BrParkinga}., Mesta: {zauzece.BrMesta}, Vreme napustanja: {zauzece.VremeNapustanja}");

                                    #region PROVERA ZAHTEVA ZA DODAVANJE NA PARKING

                                    int trazeno = zauzece.BrMesta;

                                    int sat = DateTime.Now.TimeOfDay.Hours;
                                    int minut = DateTime.Now.TimeOfDay.Minutes;
                                    string[] vreme = zauzece.VremeNapustanja.Split(':');
                                    int satKlijenta = int.Parse(vreme[0]);
                                    int minutKlijenta = int.Parse(vreme[1]);

                                    if (recnikParkinga.ContainsKey(zauzece.BrParkinga) == true && ((sat == satKlijenta && minut < minutKlijenta) || (sat < satKlijenta)))
                                    {
                                        foreach (var x in recnikParkinga)
                                        {
                                            if (x.Key == zauzece.BrParkinga)
                                            {
                                                if (x.Value.BrojZauzetih == x.Value.BrojMesta)
                                                { acceptedSockets[i].Send(Encoding.UTF8.GetBytes("Sva mesta su zauzeta!")); }
                                                else
                                                {
                                                    int temp1 = x.Value.BrojZauzetih;

                                                    x.Value.BrojZauzetih += zauzece.BrMesta;   //na parking dodajem jos zauzetih mesta koliko je korisnik trazio
                                                    if (x.Value.BrojZauzetih > x.Value.BrojMesta)//ako sam dodao vise zauzetih nego sto ima uopste mesta na parkingu
                                                    {
                                                        x.Value.BrojZauzetih = x.Value.BrojMesta;//max zauzetih ce biti koliko ima mesta na parkingu
                                                    }
                                                    temp1 = x.Value.BrojZauzetih - temp1; // a  korisniku saljem za koliko auta je zauzeto mesto
                                                    zauzece.BrMesta = temp1;//azuriram koliko je u zauzecu stvarno uzeto mesta posle kontrole

                                                    Random random = new Random();
                                                    int id = random.Next(100, 1000);

                                                    while (recnik_zauzeca.ContainsKey(id.ToString()) == true)
                                                    {
                                                        id = random.Next(100, 1000);
                                                    }

                                                    //ispis na konzolu srevera obradjen zahtev
                                                  //  Console.WriteLine($"Parking: {zauzece.BrParkinga}., Mesta: {zauzece.BrMesta}, Vreme napustanja: {zauzece.VremeNapustanja}");


                                                    recnik_zauzeca.Add(id.ToString(), zauzece); // sacuvam objekat u listu zauzeca sa izmenama posle kontrole
                                                    acceptedSockets[i].Send(Encoding.UTF8.GetBytes($"Zauzeto je {temp1} od {trazeno} trazenih mesta i vas ID racuna je: {id.ToString()}"));

                                                }
                                                break; //da ne ide dalje jer je nasao taj po id

                                            }

                                        }
                                    }
                                    else
                                    {
                                        acceptedSockets[i].Send(Encoding.UTF8.GetBytes("Uneli ste nevalidan zahtev!"));

                                    }

                                    #endregion


                                }
                            }



                        }
                    }

                   
                }
                catch (Exception ex)
                {
                  //  Console.WriteLine($"Bezveze zahtev: {ex.Message}");
                    //continue;
                }

                #endregion

            }


        }


    }

}