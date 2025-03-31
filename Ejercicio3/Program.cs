using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq; // Para estadísticas

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
        public int Prioridad { get; set; }
        public TimeSpan TiempoEspera { get; set; } // Para estadísticas

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
        static bool[] medicosDisponibles = new bool[4];
        static SemaphoreSlim maquinasDiagnostico = new SemaphoreSlim(2);
        static object lockObj = new object();
        static int turnoDiagnostico = 1;
        static Random random = new Random();
        static List<Paciente> pacientes = new List<Paciente>();
        static List<Task> tareasPacientes = new List<Task>();
        static int numeroLlegada = 1;
        static int totalUsoMaquinas = 0;

        static Program()
        {
            for (int i = 0; i < medicosDisponibles.Length; i++)
            {
                medicosDisponibles[i] = true;
            }
        }

        static async Task Main(string[] args)
        {
            DateTime inicioSimulacion = DateTime.Now;

            // AQUI CAMBIAR EL NUMERO DE PACIENTES
            await GenerarPacientes(50);
            await Task.WhenAll(tareasPacientes);

            Console.WriteLine("Simulación completada.\n");
            MostrarEstadisticas(DateTime.Now - inicioSimulacion);
        }

        static async Task GenerarPacientes(int cantidad)
        {
            for (int i = 0; i < cantidad; i++)
            {
                int id = random.Next(1, 101);
                int llegadaHospital = i * 2;
                int tiempoConsulta = random.Next(5, 16) * 1000;
                bool requiereDiagnostico = random.Next(0, 2) == 1;
                int ordenLlegada;
                lock (lockObj) { ordenLlegada = numeroLlegada++; }
                int prioridad = random.Next(1, 4);

                Paciente paciente = new Paciente(id, llegadaHospital, tiempoConsulta, requiereDiagnostico, ordenLlegada, prioridad);
                lock (lockObj) { pacientes.Add(paciente); }

                DateTime horaLlegada = DateTime.Now;
                Console.WriteLine($"Paciente {paciente.Id}. Llegado el {ordenLlegada}. Prioridad: {paciente.Prioridad}. Estado: EsperaConsulta.");

                Task tareaPaciente = Task.Run(() => AtenderPaciente(paciente, horaLlegada));
                tareasPacientes.Add(tareaPaciente);

                await Task.Delay(2000); // Simula llegada cada 2 segundos
            }
        }

        static void AtenderPaciente(Paciente paciente, DateTime horaLlegada)
        {
            int medicoAsignado = -1;
            DateTime horaInicioConsulta;
            int threadId = Thread.CurrentThread.ManagedThreadId;

            while (true)
            {
                lock (lockObj)
                {
                    pacientes.Sort((p1, p2) =>
                    {
                        if (p1.Prioridad != p2.Prioridad)
                            return p1.Prioridad.CompareTo(p2.Prioridad);
                        return p1.OrdenLlegada.CompareTo(p2.OrdenLlegada);
                    });

                    for (int i = 0; i < pacientes.Count; i++)
                    {
                        if (pacientes[i].Id == paciente.Id && pacientes[i].Estado == Estado.EsperaConsulta)
                        {
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

                Thread.Sleep(500);
            }

            horaInicioConsulta = DateTime.Now;
            paciente.TiempoEspera = horaInicioConsulta - horaLlegada;

            Console.WriteLine($"[Hilo {threadId}] Paciente {paciente.Id}. Llegado el {paciente.OrdenLlegada}. Prioridad: {paciente.Prioridad}. Estado: Consulta. Duración Espera: {paciente.TiempoEspera.Seconds} segundos. Médico: {medicoAsignado + 1}");

            Thread.Sleep(paciente.TiempoConsulta);

            lock (lockObj)
            {
                medicosDisponibles[medicoAsignado] = true;
            }

            if (paciente.RequiereDiagnostico)
            {
                paciente.Estado = Estado.EsperaDiagnostico;
                Console.WriteLine($"Paciente {paciente.Id}. Llegado el {paciente.OrdenLlegada}. Estado: EsperaDiagnostico. Requiere pruebas.");

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
                Interlocked.Add(ref totalUsoMaquinas, 15);
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

        static void MostrarEstadisticas(TimeSpan duracionTotal)
        {
            Console.WriteLine("--- FIN DEL DÍA ---");
            var prioridades = new[] { 1, 2, 3 };
            foreach (var nivel in prioridades)
            {
                int atendidos = pacientes.Count(p => p.Prioridad == nivel);
                double promedioEspera = pacientes.Where(p => p.Prioridad == nivel).Average(p => p.TiempoEspera.TotalSeconds);
                string tipo = nivel == 1 ? "Emergencias" : nivel == 2 ? "Urgencias" : "Consultas generales";
                Console.WriteLine($"- {tipo}: {atendidos} pacientes, espera promedio: {Math.Round(promedioEspera)}s");
            }

            double uso = (totalUsoMaquinas / (duracionTotal.TotalSeconds * 2)) * 100;
            Console.WriteLine($"Uso promedio de máquinas de diagnóstico: {Math.Round(uso, 1)}%\n");
        }
    }
}
