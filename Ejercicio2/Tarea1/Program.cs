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
        Finalizado
    }

    public class Paciente
    {
        public int Id { get; set; }
        public int LlegadaHospital { get; set; }
        public int TiempoConsulta { get; set; }
        public Estado Estado { get; set; }
        public bool RequiereDiagnostico { get; set; }

        public Paciente(int id, int llegadaHospital, int tiempoConsulta, bool requiereDiagnostico)
        {
            Id = id;
            LlegadaHospital = llegadaHospital;
            TiempoConsulta = tiempoConsulta;
            RequiereDiagnostico = requiereDiagnostico;
            Estado = Estado.EsperaConsulta;
        }
    }

    class Program
    {
        static bool[] medicosDisponibles = new bool[10];
        static SemaphoreSlim maquinasDiagnostico = new SemaphoreSlim(2); // 2 maquinas
        static object lockObj = new object();
        static Random random = new Random();
        static List<Paciente> pacientes = new List<Paciente>();
        static int numeroLlegada = 1;

        static Program()
        {
            for (int i = 0; i < medicosDisponibles.Length; i++)
                medicosDisponibles[i] = true;
        }

        static async Task Main(string[] args)
        {
            List<Task> tareasPacientes = new List<Task>();

            for (int i = 0; i < 6; i++) //numero de pacientes
            {
                int id = random.Next(1, 101);
                int llegadaHospital = i * 2;
                int tiempoConsulta = random.Next(5, 16) * 1000;
                bool requiereDiagnostico = random.Next(0, 2) == 1; // 50% de requerir diagnositocp
                int ordenLlegada = numeroLlegada++;

                Paciente paciente = new Paciente(id, llegadaHospital, tiempoConsulta, requiereDiagnostico);
                pacientes.Add(paciente);

                DateTime horaLlegada = DateTime.Now;
                Console.WriteLine($"Paciente {paciente.Id}. Llegado el {ordenLlegada}. Estado: EsperaConsulta.");

                int orden = ordenLlegada;
                Task tareaPaciente = Task.Run(() => AtenderPaciente(paciente, orden, horaLlegada));
                tareasPacientes.Add(tareaPaciente);

                Thread.Sleep(2000);
            }

            await Task.WhenAll(tareasPacientes);
            Console.WriteLine("Simulación completada.");
        }

        static void AtenderPaciente(Paciente paciente, int ordenLlegada, DateTime horaLlegada)
        {
            int medicoAsignado = -1;
            DateTime horaInicioConsulta;
            int threadId = Thread.CurrentThread.ManagedThreadId;

            // espera de medico
            while (true)
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
                        break;
                    }
                }

                Thread.Sleep(500);
            }

            horaInicioConsulta = DateTime.Now;
            TimeSpan duracionEspera = horaInicioConsulta - horaLlegada;
            Console.WriteLine($"[Hilo {threadId}] Paciente {paciente.Id}. Llegado el {ordenLlegada}. Estado: Consulta. Duración Espera: {duracionEspera.Seconds} segundos. Médico: {medicoAsignado + 1}");

            Thread.Sleep(paciente.TiempoConsulta);

            lock (lockObj)
            {
                medicosDisponibles[medicoAsignado] = true;
            }

            if (paciente.RequiereDiagnostico)
            {
                paciente.Estado = Estado.EsperaDiagnostico;
                Console.WriteLine($"Paciente {paciente.Id}. Llegado el {ordenLlegada}. Estado: EsperaDiagnostico. Requiere pruebas.");

                maquinasDiagnostico.Wait(); // Espera turno
                Console.WriteLine($"Paciente {paciente.Id} entra en máquina de diagnóstico.");
                Thread.Sleep(15000); // Ssimulacion diagnositoc
                Console.WriteLine($"Paciente {paciente.Id} finaliza el diagnóstico.");
                maquinasDiagnostico.Release();
            }

            paciente.Estado = Estado.Finalizado;
            TimeSpan duracionConsulta = TimeSpan.FromMilliseconds(paciente.TiempoConsulta);
            Console.WriteLine($"[Hilo {threadId}] Paciente {paciente.Id}. Llegado el {ordenLlegada}. Estado: Finalizado. Duración Consulta: {duracionConsulta.Seconds} segundos.");
        }
    }
}
