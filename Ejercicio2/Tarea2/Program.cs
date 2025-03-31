using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tarea2
{   
    //Clase enum de estados
    public enum Estado
    {
        EsperaConsulta,
        Consulta,
        EsperaDiagnostico,
        Diagnostico,
        Finalizado
    }

    public class Paciente
    {
        public int Id { get; set; }
        public int LlegadaHospital { get; set; }
        public int TiempoConsulta { get; set; }
        public Estado Estado { get; set; }
        public bool RequiereDiagnostico { get; set; }
        public int OrdenLlegada { get; set; }

        public Paciente(int id, int llegadaHospital, int tiempoConsulta, bool requiereDiagnostico, int ordenLlegada)
        {
            Id = id;
            LlegadaHospital = llegadaHospital;
            TiempoConsulta = tiempoConsulta;
            RequiereDiagnostico = requiereDiagnostico;
            Estado = Estado.EsperaConsulta;
            OrdenLlegada = ordenLlegada;
        }
    }

    class Program
    {
        //Numero de medicos, se pueden bajar para hacer pruebas.
        static bool[] medicosDisponibles = new bool[10];
        static SemaphoreSlim maquinasDiagnostico = new SemaphoreSlim(2);
        static object lockObj = new object();
        static int turnoDiagnostico = 1;
        static Random random = new Random();
        static List<Paciente> pacientes = new List<Paciente>();
        static int numeroLlegada = 1;

        static Program()
        {
            for (int i = 0; i < medicosDisponibles.Length; i++)
            {
                medicosDisponibles[i] = true;
            }
        }

        static async Task Main(string[] args)
        {
        
            List<Task> tareasPacientes = new List<Task>();

            for (int i = 0; i < 4; i++)
            {
                int id = random.Next(1, 101); //Id
                int llegadaHospital = i * 2;
                int tiempoConsulta = random.Next(5, 16) * 1000; //Tiempo de cada paciente
                bool requiereDiagnostico = random.Next(0, 2) == 1; //Llegan cada 2 segundos
                int ordenLlegada = numeroLlegada++;

                Paciente paciente = new Paciente(id, llegadaHospital, tiempoConsulta, requiereDiagnostico, ordenLlegada);
                pacientes.Add(paciente);

                DateTime horaLlegada = DateTime.Now;
                Console.WriteLine($"Paciente {paciente.Id}. Llegado el {ordenLlegada}. Estado: EsperaConsulta.");
                //Llamada a atender apacientes
                Task tareaPaciente = Task.Run(() => AtenderPaciente(paciente, horaLlegada));
                tareasPacientes.Add(tareaPaciente);

                Thread.Sleep(2000);
            }

            await Task.WhenAll(tareasPacientes);
            Console.WriteLine("Simulación completada.");
        }

        static void AtenderPaciente(Paciente paciente, DateTime horaLlegada)
        {
            int medicoAsignado = -1;
            DateTime horaInicioConsulta;
            int threadId = Thread.CurrentThread.ManagedThreadId;

            // esperar medicos con tiempo disponible
            int esperaMedicos = 0;
            while (medicoAsignado == -1 && esperaMedicos < 10)
            {
                lock (lockObj)
                {
                    List<int> disponibles = new List<int>();
                    for (int i = 0; i < medicosDisponibles.Length; i++)
                    {
                        if (medicosDisponibles[i])
                            disponibles.Add(i);
                    }

                    if (disponibles.Count > 0)
                    {
                        int index = random.Next(disponibles.Count);
                        medicoAsignado = disponibles[index];
                        medicosDisponibles[medicoAsignado] = false;
                        paciente.Estado = Estado.Consulta;
                    }
                }

                if (medicoAsignado == -1)
                {
                    Thread.Sleep(500);
                    esperaMedicos++;
                }
            }

            if (medicoAsignado == -1)
            {
                Console.WriteLine($"[Hilo {threadId}] Paciente {paciente.Id}. Llegado el {paciente.OrdenLlegada}. No se pudo asignar médico después de {esperaMedicos} intentos.");
                paciente.Estado = Estado.Finalizado;
                return;
            }

            horaInicioConsulta = DateTime.Now;
            TimeSpan duracionEspera = horaInicioConsulta - horaLlegada;

            Console.WriteLine($"[Hilo {threadId}] Paciente {paciente.Id}. Llegado el {paciente.OrdenLlegada}. Estado: Consulta. Duración Espera: {duracionEspera.Seconds} segundos. Médico: {medicoAsignado + 1}");

            Thread.Sleep(paciente.TiempoConsulta);

            lock (lockObj)
            {
                medicosDisponibles[medicoAsignado] = true;
            }

            //si es de diagnostico
            if (paciente.RequiereDiagnostico)
            {
                paciente.Estado = Estado.EsperaDiagnostico;
                Console.WriteLine($"Paciente {paciente.Id}. Llegado el {paciente.OrdenLlegada}. Estado: EsperaDiagnostico. Requiere pruebas.");

                // Esperar turno como pide el enunciado
                while (true)
                {
                    lock (lockObj)
                    {
                        if (paciente.OrdenLlegada == turnoDiagnostico)
                            break;
                    }
                    Thread.Sleep(200);
                }

                maquinasDiagnostico.Wait();

                Console.WriteLine($"Paciente {paciente.Id} entra en máquina de diagnóstico.");
                paciente.Estado = Estado.Diagnostico;
                Thread.Sleep(15000);//tarda 15 seg
                Console.WriteLine($"Paciente {paciente.Id} finaliza el diagnóstico.");

                maquinasDiagnostico.Release();

                lock (lockObj)
                {
                    turnoDiagnostico++;
                }
            }
            else
            {
                // tener en cuenta los pacientes que no requieren diagnostico tambien para liberalos 
                while (true)
                {
                    lock (lockObj)
                    {
                        if (paciente.OrdenLlegada == turnoDiagnostico)
                        {
                            turnoDiagnostico++;
                            break;
                        }
                    }
                    Thread.Sleep(200);
                }
            }

            paciente.Estado = Estado.Finalizado;
            TimeSpan duracionConsulta = TimeSpan.FromMilliseconds(paciente.TiempoConsulta);
            Console.WriteLine($"[Hilo {threadId}] Paciente {paciente.Id}. Llegado el {paciente.OrdenLlegada}. Estado: Finalizado. Duración Consulta: {duracionConsulta.Seconds} segundos.");
        }
    }
}
