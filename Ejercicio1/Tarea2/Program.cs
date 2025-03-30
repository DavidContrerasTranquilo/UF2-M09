using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tarea2
{
    public class Paciente
    {
        public int Id {get; set;}
        public int LlegadaHospital {get; set;}
        public int TiempoConsulta {get; set;}
        public int Estado {get; set;}


        public Paciente (int Id, int LlegadaHospital, int TiempoConsulta)
        {
            this.Id = Id;
            this.LlegadaHospital = LlegadaHospital;
            this.TiempoConsulta = TiempoConsulta;
        }
    }


    class Program
    {   
        //Medicos disponibles
        static bool[] medicosDisponibles = { true, true, true, true };
        static object lockObj = new object();
        static Random random = new Random();
        //Lista de pacientes
        static List<Paciente> pacientes = new List<Paciente>();
        static int numeroLlegada = 1;

        static async Task Main(string[] args)
        {
            List<Task> tareasPacientes = new List<Task>();

            for (int i = 0; i < 4; i++)
            {   
                //Asignar los valores, randomente
                int id = random.Next(1, 101);
                int llegadaHospital = i * 2;
                //Consulta entre 5 y 15 segundos
                int tiempoConsulta = random.Next(5, 16) * 1000;
                int ordenLlegada = numeroLlegada++;

                Paciente paciente = new Paciente(id, llegadaHospital, tiempoConsulta);
                pacientes.Add(paciente);

                Console.WriteLine($"Llega el Paciente {paciente.Id}. Orden de llegada: {ordenLlegada}. Estado: Espera.");

                
                int orden = ordenLlegada;   
                //Se atiende al paiente
                Task tareaPaciente = Task.Run(() => AtenderPaciente(paciente, orden));
                tareasPacientes.Add(tareaPaciente);
                //Cada dos segundos
                Thread.Sleep(2000);
            }

            await Task.WhenAll(tareasPacientes);

            Console.WriteLine("Simulación completada.");
        }

        static void AtenderPaciente(Paciente paciente, int ordenLlegada)
        {
            int medicoAsignado = -1;

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

            Console.WriteLine($"El Paciente {paciente.Id} es atendido por el Médico {medicoAsignado + 1}. Orden de llegada: {ordenLlegada}. Estado: Consulta.");

            Thread.Sleep(paciente.TiempoConsulta);

            Console.WriteLine($"El Paciente {paciente.Id} sale de la consulta. Orden de llegada: {ordenLlegada}. Estado: Finalizado.");

            paciente.Estado = 2;

            lock (lockObj)
            {
                medicosDisponibles[medicoAsignado] = true;
            }
        }
    }
}
