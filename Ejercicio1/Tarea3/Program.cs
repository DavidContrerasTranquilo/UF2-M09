using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tarea3
{
    public class Paciente
    {
        public int Id { get; set; }
        public int LlegadaHospital { get; set; }
        public int TiempoConsulta { get; set; }
        public int Estado { get; set; }

        public Paciente(int Id, int LlegadaHospital, int TiempoConsulta)
        {
            this.Id = Id;
            this.LlegadaHospital = LlegadaHospital;
            this.TiempoConsulta = TiempoConsulta;
        }
    }

    class Program
    {
        static bool[] medicosDisponibles = new bool[4]; // 10 médicos a modificar como uno quiera
        static object lockObj = new object();
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
                int id = random.Next(1, 101);
                int llegadaHospital = i * 2;
                //Tiempo de consulta calculado de 5 a 1 5
                int tiempoConsulta = random.Next(5, 16) * 1000;
                int ordenLlegada = numeroLlegada++;

                Paciente paciente = new Paciente(id, llegadaHospital, tiempoConsulta);
                pacientes.Add(paciente);
                //Empezar a contar

                DateTime horaLlegada = DateTime.Now;
                Console.WriteLine($"Paciente {paciente.Id}. Llegado el {ordenLlegada}. Estado: Espera.");

                int orden = ordenLlegada;
                Task tareaPaciente = Task.Run(() => AtenderPaciente(paciente, orden, horaLlegada));
                tareasPacientes.Add(tareaPaciente);

                Thread.Sleep(2000);
            }

            await Task.WhenAll(tareasPacientes);
            Console.WriteLine("Simulación completada.");
        }
        //metodo para anteder pacientes
        static void AtenderPaciente(Paciente paciente, int ordenLlegada, DateTime horaLlegada)
        {
            int medicoAsignado = -1;
            DateTime horaInicioConsulta;
            int threadId = Thread.CurrentThread.ManagedThreadId;

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
                        paciente.Estado = 1;
                        break;
                    }
                }

                Thread.Sleep(500);
            }

            horaInicioConsulta = DateTime.Now;
            TimeSpan duracionEspera = horaInicioConsulta - horaLlegada;

            Console.WriteLine($"[Hilo {threadId}] Paciente {paciente.Id}. Llegado el {ordenLlegada}. Estado: Consulta. Duración Espera: {duracionEspera.Seconds} segundos. Médico: {medicoAsignado + 1}");

            Thread.Sleep(paciente.TiempoConsulta);
            paciente.Estado = 2;
            //la duracion en milisegundos
            TimeSpan duracionConsulta = TimeSpan.FromMilliseconds(paciente.TiempoConsulta);
            Console.WriteLine($"[Hilo {threadId}] Paciente {paciente.Id}. Llegado el {ordenLlegada}. Estado: Finalizado. Duración Consulta: {duracionConsulta.Seconds} segundos. Médico: {medicoAsignado + 1}");

            lock (lockObj)
            {
                medicosDisponibles[medicoAsignado] = true;
            }
        }
    }
}
