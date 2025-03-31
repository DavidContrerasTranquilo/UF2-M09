using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tarea2Mejorada
{
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
        static bool[] medicosDisponibles = new bool[4]; //4 medicos
        static SemaphoreSlim maquinasDiagnostico = new SemaphoreSlim(2); // 2 maquinas
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

            for (int i = 0; i < 20; i++) // 20 pacientes
            {
                int id = random.Next(1, 101);
                int llegadaHospital = i * 2;
                int tiempoConsulta = random.Next(5, 16) * 1000;
                bool requiereDiagnostico = random.Next(0, 2) == 1;
                int ordenLlegada = numeroLlegada++;

                Paciente paciente = new Paciente(id, llegadaHospital, tiempoConsulta, requiereDiagnostico, ordenLlegada);
                pacientes.Add(paciente);

                DateTime horaLlegada = DateTime.Now;
                Console.WriteLine($"Paciente {paciente.Id}. Llegado el {ordenLlegada}. Estado: EsperaConsulta.");

                Task tareaPaciente = Task.Run(() => AtenderPaciente(paciente, horaLlegada));
                tareasPacientes.Add(tareaPaciente);

                Thread.Sleep(2000); // Llega cada 2 segundos
            }

            await Task.WhenAll(tareasPacientes);
            Console.WriteLine("Simulación completada.");
        }

        static void AtenderPaciente(Paciente paciente, DateTime horaLlegada)
        {
            int medicoAsignado = -1;
            DateTime horaInicioConsulta;
            int threadId = Thread.CurrentThread.ManagedThreadId;

            // Esperar hasta que haya medico
            while (true)
            {
                lock (lockObj)
                {
                    for (int i = 0; i < medicosDisponibles.Length; i++)
                    {
                        if (medicosDisponibles[i])
                        {
                            medicoAsignado = i;
                            medicosDisponibles[i] = false;
                            paciente.Estado = Estado.Consulta;
                            break;
                        }
                    }
                }

                if (medicoAsignado != -1)
                    break;

                Thread.Sleep(500); // Sigue esperando
            }

            horaInicioConsulta = DateTime.Now;
            TimeSpan duracionEspera = horaInicioConsulta - horaLlegada;

            Console.WriteLine($"[Hilo {threadId}] Paciente {paciente.Id}. Llegado el {paciente.OrdenLlegada}. Estado: Consulta. Duración Espera: {duracionEspera.Seconds} segundos. Médico: {medicoAsignado + 1}");

            Thread.Sleep(paciente.TiempoConsulta);

            lock (lockObj)
            {
                medicosDisponibles[medicoAsignado] = true;
            }

            // Diagnóstico si hace falta diagnostico
            if (paciente.RequiereDiagnostico)
            {
                paciente.Estado = Estado.EsperaDiagnostico;
                Console.WriteLine($"Paciente {paciente.Id}. Llegado el {paciente.OrdenLlegada}. Estado: EsperaDiagnostico. Requiere pruebas.");

                // Esperar turno de diagnostico
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
                Thread.Sleep(15000);
                Console.WriteLine($"Paciente {paciente.Id} finaliza el diagnóstico.");

                maquinasDiagnostico.Release();

                lock (lockObj)
                {
                    turnoDiagnostico++;
                }
            }
            else
            {
                // Liberar turno aunque no requiera diganostico
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
