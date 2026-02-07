using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using PomocneKlase;

namespace Client
{
    internal class Client
    {
        static void Main(string[] args)
        {
            Console.ReadKey();
            //Loopback je lokalna adr, tj. sam sebi, a meni treba adesa SERVERA tj a server je nasa lokalna loopback ustvari, moze ipconfig i da stavim za moju bas
            string adresaTCP = string.Empty;
            int portTCP = 0;


            #region UDP



            Socket clientSocketUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            EndPoint serverEndPoint = new IPEndPoint(IPAddress.Loopback, 15000);

            byte[] recBuffer = new byte[1024];
            byte[] messageBuffer = new byte[1024];
            messageBuffer = Encoding.UTF8.GetBytes("Zahtev");

            try
            {


                clientSocketUdp.SendTo(messageBuffer, serverEndPoint);

                EndPoint receiveEndPoint = new IPEndPoint(IPAddress.None, 0);

                int receivedBytes = clientSocketUdp.ReceiveFrom(recBuffer, ref receiveEndPoint); //soket nije na istoj adresi gde i ovj rezim za slanje/prijem poruka pa valjda uvek moram da pravim nov 
                                                                                                 //EndPoint TJ SAMO KAD PRIMAM PORUKU jer kad saljem mora na neku da saljem

                string response = Encoding.UTF8.GetString(recBuffer, 0, receivedBytes);
                string[] podaci = response.Split(' ');
                adresaTCP = (podaci[0]);
                portTCP = int.Parse(podaci[1]);



            }
            catch (SocketException ex)
            {
                Console.WriteLine("Ceka se podesavanje servera.");

                Console.WriteLine($"Socket greška: {ex.Message}");
            }
            clientSocketUdp.Close();



            #endregion



            if (adresaTCP != string.Empty)
            {
                #region TCP
                Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse(adresaTCP), portTCP);
                byte[] buffer = new byte[1024];


                clientSocket.Connect(serverEP);
                Console.WriteLine("Klijent je uspesno povezan sa serverom!");

                byte[] parkingInfobuffer = new byte[1024];

                try
                {
                    clientSocket.Receive(parkingInfobuffer);
                    Console.WriteLine($"{Encoding.UTF8.GetString(parkingInfobuffer)}\n");


                    BinaryFormatter formatter = new BinaryFormatter();

                    Console.WriteLine("Unesi broj parkinga koji biras: ");
                    int brParkinga = int.Parse(Console.ReadLine());

                    Console.WriteLine("Unesi broj, koliko mesta zelis zauzeti: ");
                    int brMesta = int.Parse(Console.ReadLine());

                    Console.WriteLine("Unesi vreme kada ce automobil napustiti parking U FORMATU: HH:MM): ");
                    string vremeNapustanja = Console.ReadLine();


                    Console.WriteLine("Zelis li da uneses info. o automobi-lu/ima? DA/NE ");
                    string a = (Console.ReadLine());

                    List<string> listaAutomobila = new List<string>();

                    if (a.ToLower() == "da")
                    {
                        for (int i = 0; i < brMesta; i++)
                        {
                            Console.WriteLine($"Podaci za {i + 1}. auto: ");
                            Console.WriteLine("Unesi proizvodjaca auta: ");
                            string proizvodjac = Console.ReadLine();

                            Console.WriteLine("Unesi model: ");
                            string model = Console.ReadLine();

                            Console.WriteLine("Unesi boju: ");
                            string boja = Console.ReadLine();

                            Console.WriteLine("Unesi registraciju: ");
                            string registracija = Console.ReadLine();

                            listaAutomobila.Add($"PROIZVODJAC: {proizvodjac}/ MODEL: {model}/ BOJA: {boja}/ REGISTRACIJA: {registracija}");

                            Console.WriteLine($"Podaci za {i + 1}. auto su poslati.");
                        }
                    }

                    int sat = DateTime.Now.TimeOfDay.Hours;
                    int minut = DateTime.Now.TimeOfDay.Minutes;
                    int[] vreme = {sat, minut};

                    Zauzece zauzece = new Zauzece
                    {
                        BrParkinga = brParkinga,
                        BrMesta = brMesta,
                        VremeNapustanja = vremeNapustanja,
                        AutoInfo = listaAutomobila,
                        VremeDolaska=vreme,

                    };


                    using (MemoryStream ms = new MemoryStream())
                    {
                        formatter.Serialize(ms, zauzece);
                        byte[] data = ms.ToArray();
                        clientSocket.Send(data);
                    }
                    Console.WriteLine("Upit poslat...");


                    byte[] potvrda = new byte[1024];
                    clientSocket.Receive(potvrda);
                    Console.WriteLine($"{Encoding.UTF8.GetString(potvrda)}");

                    byte[] izlaz = new byte[1024];
                    Console.WriteLine("Unesi ID racuna za izlazak u FORMATU [XXX].");
                    clientSocket.Send(Encoding.UTF8.GetBytes(Console.ReadLine()));

                    //potvrda za izlaz i cenu sa OK
                    byte[] ok = new byte[1024];
                    clientSocket.Receive(ok);
                    Console.WriteLine($"{Encoding.UTF8.GetString(ok)}");
                    while(true)
                    {
                        string kk = Console.ReadLine();
                        if (kk == "OK" || kk == "ok")
                        {
                            Console.WriteLine("Racun potvrdjen, klijent zavrsava sa radom");
                            Console.ReadKey();
                            clientSocket.Close();
                            break;
                        }
                        else
                            Console.WriteLine("Moras potvrditi! sa ok/OK!");
                       
                       
                    }
                   



                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Došlo je do greške, pri prenosu podataka: {ex.Message}");
                    // break;
                }

                

                #endregion
            }

        }

    }
}

