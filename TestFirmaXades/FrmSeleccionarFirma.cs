﻿using FirmaXadesNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestFirmaXades
{
    public partial class FrmSeleccionarFirma : Form
    {
        private FirmaXades[] _firmas = null;
        
        public FirmaXades FirmaSeleccionada
        {
            get
            {
                return _firmas[lstFirmas.SelectedIndex];
            }
        }
        
        public FrmSeleccionarFirma(FirmaXades[] firmas)
        {
            InitializeComponent();

            _firmas = firmas;

            foreach (var firma in firmas)
            {
                string textoFirma = string.Format("{0} - {1}",
                    firma.Propiedades.XadesObject.QualifyingProperties.SignedProperties.SignedSignatureProperties.SigningTime,
                    firma.Certificado.Subject);

                lstFirmas.Items.Add(textoFirma);
            }

            lstFirmas.SelectedIndex = 0;
        }
    }
}