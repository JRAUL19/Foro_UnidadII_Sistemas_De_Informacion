﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebApiAutores.Dtos.Autores
{
    public class AutorCreateDto
    {
        [Display(Name  = "Nombre")]
        [Required(ErrorMessage = "El {0} es requerido")]
        [StringLength(70, ErrorMessage = "El {0} requiere {1} caracteres")]
        public string Name { get; set; }

        [Display(Name = "direccion de imagen")]
        [Required(ErrorMessage = "La {0} es requerida")]
        public string ImageSrc { get; set; }

    }
}
