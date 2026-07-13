using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace winProyComunicacion
{
    public class GrupoInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Creador { get; set; } = string.Empty;
        public List<string> Miembros { get; set; } = new List<string>();
    }

    public class GestorGrupos
    {
        private readonly ConcurrentDictionary<string, GrupoInfo> _grupos = new ConcurrentDictionary<string, GrupoInfo>();

        public GrupoInfo CrearGrupo(string nombre, string creador, List<string> miembrosSeleccionados)
        {
            // El creador SIEMPRE se incluye en la lista de miembros, aunque no lo haya
            // seleccionado explícitamente.
            var miembros = new List<string>(miembrosSeleccionados);
            if (!miembros.Contains(creador, StringComparer.OrdinalIgnoreCase))
                miembros.Add(creador);

            string id = Guid.NewGuid().ToString("N").Substring(0, 8);
            var grupo = new GrupoInfo { Id = id, Nombre = nombre, Creador = creador, Miembros = miembros };
            _grupos[id] = grupo;
            return grupo;
        }

        public GrupoInfo? ObtenerGrupo(string id) =>
            _grupos.TryGetValue(id, out var g) ? g : null;

        // Para reenviar grupos al reconectarse un usuario
        public List<GrupoInfo> ObtenerGruposDe(string alias) =>
            _grupos.Values.Where(g => g.Miembros.Contains(alias, StringComparer.OrdinalIgnoreCase)).ToList();
    }
}
