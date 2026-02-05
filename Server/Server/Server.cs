using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using PomocneKlase;


namespace Server
{
    internal class Server
    {

        static void Main(string[] args)
        {

            string a;
            Dictionary<int, Parking> recnikParkinga = new Dictionary<int, Parking>();
            Dictionary<string, Zauzece> recnik_zauzeca = new Dictionary<string, Zauzece>();

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

            #region  UDP 


            Socket UdpServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint ServerEP = new IPEndPoint(IPAddress.Loopback, 15000);
            UdpServerSocket.Bind(ServerEP);

            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 20000);

            serverSocket.Bind(serverEP);

            serverSocket.Listen(10);


            Console.WriteLine($"Server parkinga je pokrenut!");

          

            EndPoint clientEndPoint = new IPEndPoint(IPAddress.None, 0);
            byte[] recBuffer = new byte[1024];
            while (true)
            {
                try
                {
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

                #region TCP
               

                Socket acceptedSocket = serverSocket.Accept();
                  byte[] buffer = new byte[4096];

                BinaryFormatter formatter = new BinaryFormatter();

                IPEndPoint clientEP = acceptedSocket.RemoteEndPoint as IPEndPoint;


              


                foreach (var pair in recnikParkinga)
                {
                    string parkingInfo = ($"Parking sa brojem {pair.Key} ima:\n " +
                                          $"{pair.Value.BrojZauzetih}/{pair.Value.BrojMesta} (zauzeta mesta/ukupno mesta)");

                    acceptedSocket.Send(Encoding.UTF8.GetBytes(parkingInfo));
                }

                while (true)
                {
                    try
                    {

                        /*
                                 if (acceptedSocket.Poll(1000 * 1000, SelectMode.SelectWrite))
                                 {
                        foreach (var pair in recnikParkinga)
                           {
                               string parkingInfo = ($"Parking sa brojem {pair.Key} ima:\n " +
                                                     $"{pair.Value.BrojZauzetih}/{pair.Value.BrojMesta} (zauzeta mesta/ukupno mesta)");

                               acceptedSocket.Send(Encoding.UTF8.GetBytes(parkingInfo));
                           }

                     }
                     else
                                 {
                                     Console.WriteLine("Cekam da posaljem podatke o parkingu...");
                                 }

                          */

                        if (acceptedSocket.Poll(1000 * 1000, SelectMode.SelectRead))
                        {
                            int brBajta = acceptedSocket.Receive(buffer);
                            if (brBajta == 0) break;

                            using (MemoryStream ms = new MemoryStream(buffer, 0, brBajta))
                            {
                                Zauzece zauzece = (Zauzece)formatter.Deserialize(ms);

                                Console.WriteLine($"Parking: {zauzece.BrParkinga}., Mesta: {zauzece.BrMesta}, Vreme napustanja: {zauzece.VremeNapustanja}");

                                #region PROVERA ZAHTEVA ZA DODAVANJE NA PARKING



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
                                            { acceptedSocket.Send(Encoding.UTF8.GetBytes("Sva mesta su zauzeta!")); }
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

                                                recnik_zauzeca.Add(id.ToString(), zauzece); // sacuvam objekat u listu zauzeca sa izmenama posle kontrole
                                                acceptedSocket.Send(Encoding.UTF8.GetBytes($"Zauzeto je {temp1} od {zauzece.BrMesta} trazenih mesta i vas ID racuna je: {id.ToString()}"));

                                            }
                                            break; //da ne ide dalje jer je nasao taj po id

                                        }

                                    }
                                }
                                else
                                {
                                    acceptedSocket.Send(Encoding.UTF8.GetBytes("Uneli ste nevalidan zahtev!"));

                                }

                                #endregion


                            }


                            #region IZLAZ SA PARKINGA 
                            // treba da prmim poruku s aklijenta od ID
                            byte[] IDbuffer = new byte[4024];
                            acceptedSocket.Receive(IDbuffer);

                            string izlazniID = Encoding.UTF8.GetString(IDbuffer);

                            Match match = Regex.Match(izlazniID, @"\[(\d+)\]");

                            if (match.Success)
                            {
                                izlazniID = match.Groups[1].Value;
                            }

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

                                byte[] ok = new byte[1024];
                                acceptedSocket.Send(Encoding.UTF8.GetBytes($"CENA: {cena} din. unesi OK ako potvrdjujes izlaz. "));
                                acceptedSocket.Receive(ok);

                                string k = Encoding.UTF8.GetString(ok);
                                Console.WriteLine(k);
                                k.ToLower();


                                if (k.Contains("ok") == true || k.Contains("OK") == true)
                                {


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
                                }
                                else
                                {
                                    Console.WriteLine("Klijent odbio placanje.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Nevalidan ID unesen od klijenta");
                            }


                            #endregion


                        }
                        else
                        {
                            Console.WriteLine("Cekam zahtev...");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Došlo je do greške, pri prenosu podataka: {ex.Message}");
                        break;
                    }

                }

                Console.WriteLine("Unesi kraj za gasenje Dispecera.");
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
                }

                acceptedSocket.Close();
                Console.ReadKey();


                #endregion

            }


        }


    }

}