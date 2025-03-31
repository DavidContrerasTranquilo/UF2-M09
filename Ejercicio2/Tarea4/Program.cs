using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tarea4
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
        public int Prioridad { get; set; } //prioridad

        public Paciente(int id, int llegadaHospital, int tiempoConsulta, bool requiereDiagnostico, int ordenLlegada, int prioridad)
        {
            Id = id;
            LlegadaHospital = llegadaHospital;
            TiempoConsulta = tiempoConsulta;
            RequiereDiagnostico = requiereDiagnostico;
            Estado = Estado.EsperaConsulta;
            OrdenLlegada = ordenLlegada;
            Prioridad = prioridad;
        }
    }

    class Program
    {
        static bool[] medicosDisponibles = new bool[4]; // 4 médicos
        static SemaphoreSlim maquinasDiagnostico = new SemaphoreSlim(2); // 2 máquinas
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
                int prioridad = random.Next(1, 4); // Prioridad aleatoria entre 1 y 3

                Paciente paciente = new Paciente(id, llegadaHospital, tiempoConsulta, requiereDiagnostico, ordenLlegada, prioridad);
                pacientes.Add(paciente);

                DateTime horaLlegada = DateTime.Now;
                Console.WriteLine($"Paciente {paciente.Id}. Llegado el {ordenLlegada}. Prioridad: {paciente.Prioridad}. Estado: EsperaConsulta.");

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

            // Esperar hasta que haya médico disponible y sea el turno según la prioridad
            while (true)
            {
                lock (lockObj)
                {
                    // Ordenar la lista de pacientes por prioridad y luego por orden de llegada
                    pacientes.Sort((p1, p2) =>
                    {
                        if (p1.Prioridad != p2.Prioridad)
                            return p1.Prioridad.CompareTo(p2.Prioridad);
                        return p1.OrdenLlegada.CompareTo(p2.OrdenLlegada);
                    });

                    // Buscar el paciente con la prioridad más alta y que esté en espera
                    for (int i = 0; i < pacientes.Count; i++)
                    {
                        if (pacientes[i].Id == paciente.Id && pacientes[i].Estado == Estado.EsperaConsulta)
                        {
                            // Verificar si hay médicos disponibles
                            for (int j = 0; j < medicosDisponibles.Length; j++)
                            {
                                if (medicosDisponibles[j])
                                {
                                    medicoAsignado = j;
                                    medicosDisponibles[j] = false;
                                    pacientes[i].Estado = Estado.Consulta;
                                    break;
                                }
                            }
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

            Console.WriteLine($"[Hilo {threadId}] Paciente {paciente.Id}. Llegado el {paciente.OrdenLlegada}. Prioridad: {paciente.Prioridad}. Estado: Consulta. Duración Espera: {duracionEspera.Seconds} segundos. Médico: {medicoAsignado + 1}");

            Thread.Sleep(paciente.TiempoConsulta);

            lock (lockObj)
            {
                medicosDisponibles[medicoAsignado] = true;
            }

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
            Console.WriteLine($"[Hilo {threadId}] Paciente {paciente.Id}. Llegado el {paciente.OrdenLlegada}. Prioridad: {paciente.Prioridad}. Estado: Finalizado. Duración Consulta: {duracionConsulta.Seconds} segundos.");
        }
    }
}