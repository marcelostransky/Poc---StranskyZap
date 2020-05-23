using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StranskyZapWeb.Models
{
    public class Mensagem
    {
        public int Id { get; set; }
        public string NomeGrupo { get; set; }
        public string UsuarioId { get; set; }
        public string Usuario { get; set; }
        public string Texto { get; set; }
        public DateTime? DataCriacao { get; set; }

    }
}
