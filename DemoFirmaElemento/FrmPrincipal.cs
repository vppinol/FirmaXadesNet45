﻿// --------------------------------------------------------------------------------------------------------------------
// FrmPrincipal.cs
//
// FirmaXadesNet - Demo de generación de firma internally detached especificando el nodo de destino de la firma
// Copyright (C) 2016 Dpto. de Nuevas Tecnologías de la Dirección General de Urbanismo del Ayto. de Cartagena
//
// This program is free software: you can redistribute it and/or modify
// it under the +terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/. 
//
// E-Mail: informatica@gemuc.es
// 
// --------------------------------------------------------------------------------------------------------------------

using FirmaXadesNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace DemoFirmaElemento
{
    public partial class FrmPrincipal : Form
    {
        public FrmPrincipal()
        {
            InitializeComponent();
        }

        private void btnFirmar_Click(object sender, EventArgs e)
        {
            FirmaXades firmaXades = new FirmaXades();
            string ficheroXml = Application.StartupPath + "\\xsdBOE-A-2011-13169_ex_XAdES_Internally_detached.xml";

            XmlDocument documento = new XmlDocument();
            documento.Load(ficheroXml);

            firmaXades.SetContentInternallyDetached(documento, "CONTENT-12ef114d-ac6c-4da3-8caf-50379ed13698", "text/xml");

            Dictionary<string, string> namespaces = new Dictionary<string, string>();
            namespaces.Add("enidoc", "http://administracionelectronica.gob.es/ENI/XSD/v1.0/documento-e");
            namespaces.Add("enidocmeta", "http://administracionelectronica.gob.es/ENI/XSD/v1.0/documento-e/metadatos");
            namespaces.Add("enids", "http://administracionelectronica.gob.es/ENI/XSD/v1.0/firma");
            namespaces.Add("enifile", "http://administracionelectronica.gob.es/ENI/XSD/v1.0/documento-e/contenido");

            firmaXades.SetSignatureDestination("enidoc:documento/enids:firmas/enids:firma/enids:ContenidoFirma/enids:FirmaConCertificado", namespaces);

            firmaXades.Sign(firmaXades.SelectCertificate());

            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                using (FileStream fs = new FileStream(saveFileDialog1.FileName, FileMode.Create))
                {
                    firmaXades.Save(fs);
                }

                MessageBox.Show("Fichero guardado correctamente.");
            }
        }
    }
}
