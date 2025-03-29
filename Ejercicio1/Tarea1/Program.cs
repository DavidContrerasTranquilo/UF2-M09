﻿using System;
using System.Threading;
using System.Collections.Generic;

namespace ejercicio1
{
    class Program
    {
        static bool[] medicosDisponibles = { true, true, true, true };
        static object lockObj = new object();
        static Random random = new Random();

        static void Main(string[] args)
        {
            for (int i = 1; i <= 4; i++)
            {
                int pacienteId = i;
                Thread.Sleep(2000);
                Console.WriteLine($"Llega el Paciente {pacienteId}");

                Thread pacienteThread = new Thread(() => AtenderPaciente(pacienteId));
                pacienteThread.Start();
            }

            Thread.Sleep(15000); 
            Console.WriteLine("Simulación completada.");
        }

        static void AtenderPaciente(int pacienteId)
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
                        medicoAsignado += 1; 
                        break;
                    }
                }

                Thread.Sleep(500);
            }

            Console.WriteLine($"El Paciente {pacienteId} es atendido por el Médico {medicoAsignado}");
            Thread.Sleep(10000);
            Console.WriteLine($"El Paciente {pacienteId} sale de la consulta");

            lock (lockObj)
            {
                medicosDisponibles[medicoAsignado - 1] = true;
            }
        }
    }
}
