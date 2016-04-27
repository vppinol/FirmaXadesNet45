﻿// --------------------------------------------------------------------------------------------------------------------
// FirmaXades.cs
//
// FirmaXadesNet - Librería para la generación de firmas XADES
// Copyright (C) 2014 Dpto. de Nuevas Tecnologías de la Concejalía de Urbanismo de Cartagena
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
// Contact info: J. Arturo Aguado
// Email: informatica@gemuc.es
// 
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Xades;
using System.IO;
using Org.BouncyCastle.Tsp;
using System.Net;
using Org.BouncyCastle.Math;
using System.Collections;
using FirmaXadesNet;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.X509.Store;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.Cmp;
using System.Reflection;

namespace FirmaXadesNet
{
    public enum TipoAlgoritmo
    {
        SHA1 = 0,
        SHA256 = 1,
        SHA512 = 2
    }

    public class FirmaXades : IDisposable
    {
        private X509Certificate2 _certificate;
        private X509Chain _chain;
        private XadesSignedXml _xadesSignedXml;
        private XmlDocument _xmlDocument;
        private RSACryptoServiceProvider _rsaKey;
        private string _mimeType;
        private string _signatureId;
        private string _signatureValueId;
        private string _objectReference;
        private string _policyIdentifier;
        private string _policyUri;
        private string _policyHash;
        private string _tsaServer;
        private string _ocspServer;
        
        private TipoAlgoritmo _algoritmoFirma;
        private TipoAlgoritmo _algoritmoReferencias;

        private string _algoritmoFirmaUrl;
        private string _algoritmoRefUrl;

        private bool _liberarCryptoProvider;

        private List<string> _certificatesChecked;

        private const string ReferenciaSHA1 = "http://www.w3.org/2000/09/xmldsig#sha1";
        private const string ReferenciaSHA256 = "http://www.w3.org/2001/04/xmlenc#sha256";
        private const string ReferenciaSHA512 = "http://www.w3.org/2001/04/xmlenc#sha512";


        private const string FirmaSHA1 = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
        private const string FirmaSHA256 = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
        private const string FirmaSHA512 = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha512";


        /// <summary>
        /// Establece la URL del servidor para el sellado de tiempo.
        /// </summary>
        public string URLServidorTSA
        {
            get
            {
                return _tsaServer;
            }

            set
            {
                _tsaServer = value;
            }
        }

        /// <summary>
        /// Establece el servidor OCSP por defecto a utilizar si el certificado emisor
        /// no contiene ninguna URL para validar el certificado.
        /// </summary>
        public string ServidorOCSP
        {
            get
            {
                return _ocspServer;
            }

            set
            {
                _ocspServer = value;
            }
        }

        /// <summary>
        /// Establece el identificador de la política de firma
        /// </summary>
        public string PolicyIdentifier
        {
            get
            {
                return _policyIdentifier;
            }

            set
            {
                _policyIdentifier = value;
            }
        }

        /// <summary>
        /// Establece el hash en base 64 de la politica de firma
        /// </summary>
        public string PolicyHash
        {
            get
            {
                return _policyHash;
            }
            set
            {
                _policyHash = value;
            }
        }


        /// <summary>
        /// Establece la URI de la politica de firma
        /// </summary>
        public string PolicyUri
        {
            get
            {
                return _policyUri;
            }

            set
            {
                _policyUri = value;
            }

        }

        /// <summary>
        /// Tipo de algoritmo para la huella de la firma
        /// </summary>
        public TipoAlgoritmo AlgoritmoFirma
        {
            get
            {
                return _algoritmoFirma;
            }
            set
            {
                _algoritmoFirma = value;

                switch (_algoritmoFirma)
                {
                    case TipoAlgoritmo.SHA1:
                        _algoritmoFirmaUrl = FirmaSHA1;
                        break;

                    case TipoAlgoritmo.SHA256:
                        _algoritmoFirmaUrl = FirmaSHA256;
                        break;

                    case TipoAlgoritmo.SHA512:
                        _algoritmoFirmaUrl = FirmaSHA512;
                        break;
                }
            }
        }

        /// <summary>
        /// Tipo de algoritmo para la huella de las referencias
        /// </summary>
        public TipoAlgoritmo AlgoritmoReferencias
        {
            get
            {
                return _algoritmoReferencias;
            }

            set
            {
                _algoritmoReferencias = value;

                switch (_algoritmoReferencias)
                {
                    case TipoAlgoritmo.SHA1:
                        _algoritmoRefUrl = ReferenciaSHA1;
                        break;

                    case TipoAlgoritmo.SHA256:
                        _algoritmoRefUrl = ReferenciaSHA256;
                        break;

                    case TipoAlgoritmo.SHA512:
                        _algoritmoRefUrl = ReferenciaSHA512;
                        break;
                }
            }
        }

        /// <summary>
        /// Certificado empleado para la firma
        /// </summary>
        public X509Certificate2 Certificado
        {
            get
            {
                return _certificate;
            }
        }

        /// <summary>
        /// Devuelve el documento XML resultante
        /// </summary>
        public XmlDocument Documento
        {
            get
            {
                return _xmlDocument;
            }
        }

        public XadesSignedXml Propiedades
        {
            get
            {
                return _xadesSignedXml;
            }
        }

        public FirmaXades()
        {
            this.AlgoritmoFirma = TipoAlgoritmo.SHA512;
            this.AlgoritmoReferencias = TipoAlgoritmo.SHA512;
        }


        /// <summary>
        /// Especifica el nodo en el cual se añadira la firma
        /// </summary>
        /// <param name="rutaXPath"></param>
        /// <param name="namespaces"></param>
        public void EstablecerNodoDestinoFirma(string rutaXPath, Dictionary<string, string> namespaces = null)
        {
            XmlNode nodo;

            if (namespaces != null)
            {
                XmlNamespaceManager xmlnsMgr = new XmlNamespaceManager(_xmlDocument.NameTable);
                foreach (var item in namespaces)
                {
                    xmlnsMgr.AddNamespace(item.Key, item.Value);
                }

                nodo = _xmlDocument.SelectSingleNode(rutaXPath, xmlnsMgr);
            }
            else
            {
                nodo = _xmlDocument.SelectSingleNode(rutaXPath);
            }

            if (nodo == null)
            {
                throw new Exception("No se ha encontrado el nodo de destino.");
            }

            _xadesSignedXml.SignatureNodeDestination = (XmlElement)nodo;
        }

        /// <summary>
        /// Carga el documento XML especificado y establece para firmar el elemento especificado en idAFirmar
        /// </summary>
        /// <param name="contenidoXml"></param>
        /// <param name="idAFirmar"></param>
        /// <param name="mimeType"></param>
        public void FirmarElementoInternallyDetached(string contenidoXml, string idAFirmar, string mimeType)
        {
            _xmlDocument = new XmlDocument();
            _xmlDocument.PreserveWhitespace = true;
            _xmlDocument.LoadXml(contenidoXml);

            Reference reference = new Reference();

            reference.Uri = "#" + idAFirmar;
            reference.Id = "Reference-" + Guid.NewGuid().ToString();

            _objectReference = reference.Id;
            _mimeType = mimeType;

            if (mimeType == "text/xml")
            {
                XmlDsigC14NTransform transform = new XmlDsigC14NTransform();
                reference.AddTransform(transform);
            }
            else
            {
                XmlDsigBase64Transform transform = new XmlDsigBase64Transform();
                reference.AddTransform(transform);
            }

            _xadesSignedXml = new XadesSignedXml(_xmlDocument);

            _xadesSignedXml.AddReference(reference);
        }

        /// <summary>
        /// Inserta un documento para generar una firma internally detached.
        /// </summary>
        /// <param name="contenido"></param>
        /// <param name="mimeType"></param>
        public void InsertarFicheroInternallyDetached(byte[] contenido, string mimeType, string nombreFichero = null)
        {
            _xmlDocument = new XmlDocument();

            XmlElement rootElement = _xmlDocument.CreateElement("DOCFIRMA");
            _xmlDocument.AppendChild(rootElement);

            string id = "CONTENT-" + Guid.NewGuid().ToString();

            Reference reference = new Reference();

            reference.Uri = "#" + id;
            reference.Id = "Reference-" + Guid.NewGuid().ToString();

            _objectReference = reference.Id;
            _mimeType = mimeType;

            XmlElement contentElement = _xmlDocument.CreateElement("CONTENT");

            if (mimeType == "text/xml")
            {
                XmlDocument doc = new XmlDocument();
                doc.PreserveWhitespace = true;
                doc.Load(new MemoryStream(contenido));

                contentElement.InnerXml = doc.DocumentElement.OuterXml;

                XmlDsigC14NTransform transform = new XmlDsigC14NTransform();
                reference.AddTransform(transform);
            }
            else if (mimeType == "hash/sha256")
            {
                contentElement.SetAttribute("Encoding", "http://www.w3.org/2000/09/xmldsig#base64");
                contentElement.SetAttribute("MimeType", mimeType);

                if (!string.IsNullOrEmpty(nombreFichero))
                {
                    contentElement.SetAttribute("URI", Path.GetFileName(nombreFichero));
                }

                using (SHA256 sha2 = SHA256.Create())
                {
                    contentElement.InnerText = Convert.ToBase64String(sha2.ComputeHash(contenido));
                }

                XmlDsigBase64Transform transform = new XmlDsigBase64Transform();
                reference.AddTransform(transform);
            }
            else
            {
                contentElement.SetAttribute("Encoding", "http://www.w3.org/2000/09/xmldsig#base64");
                contentElement.InnerText = Convert.ToBase64String(contenido);

                XmlDsigBase64Transform transform = new XmlDsigBase64Transform();
                reference.AddTransform(transform);
            }

            contentElement.SetAttribute("Id", id);

            rootElement.AppendChild(contentElement);

            _xadesSignedXml = new XadesSignedXml(_xmlDocument);

            _xadesSignedXml.AddReference(reference);

        }


        /// <summary>
        /// Inserta un documento para generar una firma internally detached.
        /// </summary>
        /// <param name="nombreFichero"></param>
        /// <param name="mimeType"></param>
        public void InsertarFicheroInternallyDetached(string nombreFichero, string mimeType)
        {
            byte[] contenido = File.ReadAllBytes(nombreFichero);

            if (mimeType.StartsWith("hash"))
            {
                InsertarFicheroInternallyDetached(contenido, mimeType, nombreFichero);
            }
            else
            {
                InsertarFicheroInternallyDetached(contenido, mimeType);
            }
        }

        /// <summary>
        /// Inserta un documento para generar una firma externally detached.
        /// </summary>
        /// <param name="nombreFichero"></param>
        public void InsertarFicheroExternallyDetached(string nombreFichero)
        {
            Reference reference = new Reference();

            _xmlDocument = new XmlDocument();
            _xadesSignedXml = new XadesSignedXml();

            reference.Uri = "file://" + nombreFichero.Replace("\\", "/");
            reference.Id = "Reference-" + Guid.NewGuid().ToString();

            if (reference.Uri.EndsWith(".xml") || reference.Uri.EndsWith(".XML"))
            {
                _mimeType = "text/xml";
                reference.AddTransform(new XmlDsigC14NTransform());
            }

            _objectReference = reference.Id;

            _xadesSignedXml.AddReference(reference);
        }

        /// <summary>
        /// Inserta un documento XML para generar una firma enveloped.
        /// </summary>
        /// <param name="nombreFicheroXML"></param>
        public void InsertarFicheroEnveloped(string nombreFicheroXML)
        {
            _xmlDocument = new XmlDocument();
            _xmlDocument.PreserveWhitespace = true;
            _xmlDocument.Load(nombreFicheroXML);

            CrearDocumentoEnveloped();
        }

        /// <summary>
        /// Inserta un contenido XML para generar una firma enveloped.
        /// </summary>
        /// <param name="contenidoXML"></param>
        public void InsertarContenidoEnveloped(string contenidoXML)
        {
            _xmlDocument = new XmlDocument();
            _xmlDocument.PreserveWhitespace = true;
            _xmlDocument.LoadXml(contenidoXML);

            CrearDocumentoEnveloped();
        }

        /// <summary>
        /// Construye el documento enveloped
        /// </summary>
        private void CrearDocumentoEnveloped()
        {
            Reference reference = new Reference();

            _xadesSignedXml = new XadesSignedXml(_xmlDocument);

            reference.Id = "Reference-" + Guid.NewGuid().ToString();
            reference.Uri = "";

            for (int i = 0; i < _xmlDocument.DocumentElement.Attributes.Count; i++)
            {
                if (_xmlDocument.DocumentElement.Attributes[i].Name.Equals("id", StringComparison.InvariantCultureIgnoreCase))
                {
                    reference.Uri = "#" + _xmlDocument.DocumentElement.Attributes[i].Value;
                    break;
                }
            }

            XmlDsigEnvelopedSignatureTransform xmlDsigEnvelopedSignatureTransform = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(xmlDsigEnvelopedSignatureTransform);

            _objectReference = reference.Id;

            _xadesSignedXml.AddReference(reference);
        }

        /// <summary>
        /// Inserta un contenido XML para generar una firma enveloping.
        /// </summary>
        /// <param name="contenidoXML"></param>
        public void InsertarContenidoEnveloping(string contenidoXML)
        {
            Reference reference = new Reference();

            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(contenidoXML);
            _xadesSignedXml = new XadesSignedXml();

            //Add an object
            string dataObjectId = "DataObject-" + Guid.NewGuid().ToString();
            System.Security.Cryptography.Xml.DataObject dataObject = new System.Security.Cryptography.Xml.DataObject();
            dataObject.Data = doc.ChildNodes;
            dataObject.Id = dataObjectId;
            _xadesSignedXml.AddObject(dataObject);

            reference.Id = "Reference-" + Guid.NewGuid().ToString();
            reference.Uri = "#" + dataObjectId;
            reference.Type = SignedXml.XmlDsigNamespaceUrl + "Object";

            _objectReference = reference.Id;
            _mimeType = "text/xml";

            _xadesSignedXml.AddReference(reference);
        }

        private void ActualizarDocumento()
        {
            if (_xmlDocument == null)
            {
                _xmlDocument = new XmlDocument();
            }

            if (_xmlDocument.DocumentElement != null)
            {
                XmlNode xmlNode = _xmlDocument.SelectSingleNode("//*[@Id='" + _xadesSignedXml.Signature.Id + "']");

                if (xmlNode != null)
                {

                    XmlNamespaceManager nm = new XmlNamespaceManager(_xmlDocument.NameTable);
                    nm.AddNamespace("xades", XadesSignedXml.XadesNamespaceUri);
                    nm.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);

                    XmlNode xmlQPNode = xmlNode.SelectSingleNode("ds:Object/xades:QualifyingProperties", nm);
                    XmlNode xmlUnsingedPropertiesNode = xmlNode.SelectSingleNode("ds:Object/xades:QualifyingProperties/xades:UnsignedProperties", nm);

                    if (xmlUnsingedPropertiesNode != null)
                    {
                        xmlUnsingedPropertiesNode.InnerXml = _xadesSignedXml.XadesObject.QualifyingProperties.UnsignedProperties.GetXml().InnerXml;
                    }
                    else
                    {
                        xmlUnsingedPropertiesNode = _xmlDocument.ImportNode(_xadesSignedXml.XadesObject.QualifyingProperties.UnsignedProperties.GetXml(), true);
                        xmlQPNode.AppendChild(xmlUnsingedPropertiesNode);
                    }

                }
                else
                {
                    XmlElement xmlSigned = _xadesSignedXml.GetXml();

                    MemoryStream canonicalizedStream;
                    XmlDsigC14NTransform xmlDsigC14NTransform;
                    UTF8Encoding encoding = new UTF8Encoding(false);
                    byte[] buffer = encoding.GetBytes(xmlSigned.OuterXml);

                    using (MemoryStream ms = new MemoryStream(buffer))
                    {
                        xmlDsigC14NTransform = new XmlDsigC14NTransform();
                        xmlDsigC14NTransform.LoadInput(ms);
                        canonicalizedStream = (MemoryStream)xmlDsigC14NTransform.GetOutput(typeof(Stream));
                        canonicalizedStream.Flush();
                    }

                    XmlDocument doc = new XmlDocument();
                    doc.PreserveWhitespace = true;
                    doc.Load(canonicalizedStream);

                    XmlNode canonSignature = _xmlDocument.ImportNode(doc.DocumentElement, true);

                    _xadesSignedXml.GetSignatureElement().AppendChild(canonSignature);

                    canonicalizedStream.Dispose();
                }
            }
            else
            {
                _xmlDocument.LoadXml(_xadesSignedXml.GetXml().OuterXml);
            }
        }

        /// <summary>
        /// Realiza el proceso de firmado
        /// </summary>
        public void Firmar(X509Certificate2 certificadoFirma, TipoAlgoritmo? algoritmoFirma = null)
        {
            if (certificadoFirma == null)
            {
                throw new Exception("Es necesario un certificado válido para la firma.");
            }

            if (algoritmoFirma.HasValue)
            {
                this.AlgoritmoFirma = algoritmoFirma.Value;
            }

            _signatureId = "Signature-" + Guid.NewGuid().ToString();
            _signatureValueId = "SignatureValue-" + Guid.NewGuid().ToString();

            _certificate = certificadoFirma;
            _chain = new X509Chain();

            _chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
            _chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            _chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 0, 30);
            _chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

            if (!_chain.Build(_certificate))
            {
                throw new Exception("No se he podido verificar la cadena del certificado.");
            }

            InsertarInfoCertificado();
            InsertarInfoXades(_mimeType);

            foreach (Reference reference in _xadesSignedXml.SignedInfo.References)
            {
                reference.DigestMethod = _algoritmoRefUrl;
            }

            _xadesSignedXml.SignedInfo.SignatureMethod = _algoritmoFirmaUrl;

            ComputarFirma();

            ActualizarDocumento();

        }

        /// <summary>
        /// Añade una firma al documento
        /// </summary>
        /// <param name="certificadoFirma"></param>
        public void CoFirmar(X509Certificate2 certificadoFirma, TipoAlgoritmo? algoritmoFirma = null)
        {
            if (_xadesSignedXml == null)
            {
                throw new Exception("No hay ninguna firma XADES creada previamente.");
            }

            if (certificadoFirma == null)
            {
                throw new Exception("Es necesario un certificado válido para la firma.");
            }

            Reference refContenido = _xadesSignedXml.SignedInfo.References[0] as Reference;

            if (refContenido == null)
            {
                throw new Exception("No se ha podido encontrar la referencia del contenido firmado.");
            }

            if (_xadesSignedXml.XadesObject.QualifyingProperties.SignedProperties.SignedDataObjectProperties.DataObjectFormatCollection.Count > 0)
            {
                foreach (DataObjectFormat dof in _xadesSignedXml.XadesObject.QualifyingProperties.SignedProperties.SignedDataObjectProperties.DataObjectFormatCollection)
                {
                    if (dof.ObjectReferenceAttribute == ("#" + refContenido.Id))
                    {
                        _mimeType = dof.MimeType;
                        break;
                    }
                }
            }

            _xadesSignedXml = new XadesSignedXml(_xmlDocument);

            refContenido.Id = "Reference-" + Guid.NewGuid().ToString();
            _xadesSignedXml.AddReference(refContenido);

            _objectReference = refContenido.Id;

            Firmar(certificadoFirma, algoritmoFirma);
        }


        /// <summary>
        /// Realiza la contrafirma de la firma actualmente cargada
        /// </summary>
        /// <param name="certificadoFirma"></param>
        public void ContraFirma(X509Certificate2 certificadoFirma, TipoAlgoritmo? algoritmoFirma = null)
        {
            string signatureId = "Signature-" + Guid.NewGuid().ToString();

            if (_xadesSignedXml == null)
            {
                throw new Exception("No hay ninguna firma XADES cargada previamente.");
            }

            if (certificadoFirma == null)
            {
                throw new Exception("Es necesario un certificado válido para la firma.");
            }

            if (algoritmoFirma.HasValue)
            {
                this.AlgoritmoFirma = algoritmoFirma.Value;
            }

            _certificate = certificadoFirma;

            XadesSignedXml counterSignature = new XadesSignedXml(_xmlDocument);

            ObtenerCryptoServiceProvider();

            counterSignature.SigningKey = _rsaKey;

            Reference reference = new Reference();
            reference.Uri = "#" + _xadesSignedXml.SignatureValueId;
            reference.Id = "Reference-" + Guid.NewGuid().ToString();
            reference.Type = "http://uri.etsi.org/01903#CountersignedSignature";
            reference.AddTransform(new XmlDsigC14NTransform());
            counterSignature.AddReference(reference);
            _objectReference = reference.Id;

            KeyInfo keyInfo = new KeyInfo();
            keyInfo.Id = "KeyInfoId-" + signatureId;
            keyInfo.AddClause(new KeyInfoX509Data((X509Certificate)certificadoFirma));
            keyInfo.AddClause(new RSAKeyValue((RSA)_rsaKey));
            counterSignature.KeyInfo = keyInfo;

            Reference referenceKeyInfo = new Reference();
            referenceKeyInfo.Id = "ReferenceKeyInfo-" + signatureId;
            referenceKeyInfo.Uri = "#KeyInfoId-" + signatureId;
            counterSignature.AddReference(referenceKeyInfo);

            counterSignature.Signature.Id = signatureId;
            counterSignature.SignatureValueId = "SignatureValue-" + Guid.NewGuid().ToString();

            XadesObject counterSignatureXadesObject = new XadesObject();
            counterSignatureXadesObject.Id = "CounterSignatureXadesObject-" + Guid.NewGuid().ToString();
            counterSignatureXadesObject.QualifyingProperties.Target = "#" + signatureId;
            counterSignatureXadesObject.QualifyingProperties.SignedProperties.Id = "SignedProperties-" + signatureId;

            InsertarPropiedadesFirma(counterSignatureXadesObject.QualifyingProperties.SignedProperties.SignedSignatureProperties,
                counterSignatureXadesObject.QualifyingProperties.SignedProperties.SignedDataObjectProperties,
                counterSignatureXadesObject.QualifyingProperties.UnsignedProperties.UnsignedSignatureProperties,
                "text/xml", certificadoFirma);

            counterSignature.AddXadesObject(counterSignatureXadesObject);

            foreach (Reference signReference in counterSignature.SignedInfo.References)
            {
                signReference.DigestMethod = _algoritmoRefUrl;
            }

            counterSignature.AddXadesNamespace = true;
            counterSignature.ComputeSignature();

            UnsignedProperties unsignedProperties = _xadesSignedXml.UnsignedProperties;
            unsignedProperties.UnsignedSignatureProperties.CounterSignatureCollection.Add(counterSignature);
            _xadesSignedXml.UnsignedProperties = unsignedProperties;

            ActualizarDocumento();

            _xadesSignedXml = new XadesSignedXml(_xmlDocument);

            XmlNode xmlNode = _xmlDocument.SelectSingleNode("//*[@Id='" + signatureId + "']");

            _xadesSignedXml.LoadXml((XmlElement)xmlNode);

            _certificate = certificadoFirma;

            _chain = new X509Chain();
            _chain.Build(_certificate);

        }

        /// <summary>
        /// Guardar la firma en el fichero especificado.
        /// </summary>
        /// <param name="nombreFichero"></param>
        public void GuardarFirma(string nombreFichero)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = new UTF8Encoding();
            using (var writer = XmlWriter.Create(nombreFichero, settings))
            {
                this.Documento.Save(writer);
            }
        }

        /// <summary>
        /// Guarda la firma en el destino especificado
        /// </summary>
        /// <param name="destino"></param>
        public void GuardarFirma(Stream destino)
        {
            this.Documento.Save(destino);
        }


        /// <summary>
        /// Carga un archivo de firma.
        /// </summary>
        /// <param name="origen"></param>
        /// <returns></returns>
        public static FirmaXades[] CargarFirma(Stream origen)
        {
            XmlDocument documento = new XmlDocument();
            documento.PreserveWhitespace = true;
            documento.Load(origen);

            return CargarFirma(documento);
        }


        /// <summary>
        /// Carga un archivo de firma.
        /// </summary>
        /// <param name="nombreFichero"></param>
        public static FirmaXades[] CargarFirma(XmlDocument documento)
        {
            XmlNodeList signatureNodeList = documento.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl);

            if (signatureNodeList.Count == 0)
            {
                throw new Exception("No se ha encontrado ninguna firma.");
            }

            List<FirmaXades> firmas = new List<FirmaXades>();

            foreach (var signatureNode in signatureNodeList)
            {
                FirmaXades firma = new FirmaXades();
                firma._xmlDocument = documento;

                firma._xadesSignedXml = new XadesSignedXml(firma._xmlDocument);
                firma._xadesSignedXml.LoadXml((XmlElement)signatureNode);

                if (firma._xadesSignedXml.SignedInfo.SignatureMethod == FirmaSHA1)
                {
                    firma.AlgoritmoFirma = TipoAlgoritmo.SHA1;
                }
                else if (firma._xadesSignedXml.SignedInfo.SignatureMethod == FirmaSHA256)
                {
                    firma.AlgoritmoFirma = TipoAlgoritmo.SHA256;
                }
                else if (firma._xadesSignedXml.SignedInfo.SignatureMethod == FirmaSHA512)
                {
                    firma.AlgoritmoFirma = TipoAlgoritmo.SHA512;
                }

                XmlNode keyXml = firma._xadesSignedXml.KeyInfo.GetXml().GetElementsByTagName("X509Certificate", SignedXml.XmlDsigNamespaceUrl)[0];
                
                firma._certificate = new X509Certificate2(Convert.FromBase64String(keyXml.InnerText));
                
                firma._chain = new X509Chain();
                firma._chain.Build(firma._certificate);

                firma._xadesSignedXml.FindContentElement();

                firmas.Add(firma);
            }

            return firmas.ToArray();
        }

        /// <summary>
        /// Selecciona un certificado del almacén de certificados
        /// </summary>
        /// <returns></returns>
        public X509Certificate2 SeleccionarCertificado()
        {
            X509Certificate2 cert = null;

            try
            {
                // Open the store of personal certificates.
                X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
                X509Certificate2Collection fcollection = (X509Certificate2Collection)collection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
                X509Certificate2Collection scollection = X509Certificate2UI.SelectFromCollection(fcollection, "Firma XAdES", "Seleccione un certificado", X509SelectionFlag.SingleSelection);

                if (scollection != null && scollection.Count == 1)
                {
                    cert = scollection[0];

                    if (cert.HasPrivateKey == false)
                    {
                        throw new Exception("El certificado no tiene asociada una clave privada.");
                    }
                }

                store.Close();
            }
            catch (Exception)
            {
                throw new Exception("No se ha podido obtener la clave privada.");
            }

            return cert;
        }


        private void InsertarInfoXades(string mimeType)
        {
            _xadesSignedXml.Signature.Id = _signatureId;
            XadesObject xadesObject = new XadesObject();
            xadesObject.Id = "XadesObjectId-" + Guid.NewGuid().ToString();
            xadesObject.QualifyingProperties.Id = "QualifyingProperties-" + Guid.NewGuid().ToString();
            xadesObject.QualifyingProperties.Target = "#" + _signatureId;
            xadesObject.QualifyingProperties.SignedProperties.Id = "SignedProperties-" + _signatureId;

            InsertarPropiedadesFirma(
                xadesObject.QualifyingProperties.SignedProperties.SignedSignatureProperties,
                xadesObject.QualifyingProperties.SignedProperties.SignedDataObjectProperties,
                xadesObject.QualifyingProperties.UnsignedProperties.UnsignedSignatureProperties,
                mimeType, _certificate);

            _xadesSignedXml.AddXadesObject(xadesObject);
        }


        private void ObtenerCryptoServiceProvider()
        {
            string providerName = "Microsoft Enhanced RSA and AES Cryptographic Provider";
            int providerType = 24;

            var key = (RSACryptoServiceProvider)_certificate.PrivateKey;

            if (_rsaKey != null &&
                key.CspKeyContainerInfo.UniqueKeyContainerName == _rsaKey.CspKeyContainerInfo.UniqueKeyContainerName)
                return;
            
            if (key.CspKeyContainerInfo.ProviderName == "Microsoft Strong Cryptographic Provider" ||
                key.CspKeyContainerInfo.ProviderName == "Microsoft Enhanced Cryptographic Provider v1.0" ||
                key.CspKeyContainerInfo.ProviderName == "Microsoft Base Cryptographic Provider v1.0")
            {
                Type CspKeyContainerInfo_Type = typeof(CspKeyContainerInfo);

                FieldInfo CspKeyContainerInfo_m_parameters = CspKeyContainerInfo_Type.GetField("m_parameters", BindingFlags.NonPublic | BindingFlags.Instance);
                CspParameters parameters = (CspParameters)CspKeyContainerInfo_m_parameters.GetValue(key.CspKeyContainerInfo);

                var cspparams = new CspParameters(providerType, providerName, key.CspKeyContainerInfo.KeyContainerName);
                cspparams.Flags = parameters.Flags;
                _rsaKey = new RSACryptoServiceProvider(cspparams);

                _liberarCryptoProvider = true;
            }
            else
            {
                _rsaKey = key;
                _liberarCryptoProvider = false;
            }
        }


        private void InsertarInfoCertificado()
        {
            ObtenerCryptoServiceProvider();

            _xadesSignedXml.SigningKey = _rsaKey;

            KeyInfo keyInfo = new KeyInfo();
            keyInfo.Id = "KeyInfoId-" + _signatureId;
            keyInfo.AddClause(new KeyInfoX509Data((X509Certificate)_certificate));
            keyInfo.AddClause(new RSAKeyValue((RSA)_rsaKey));

            _xadesSignedXml.KeyInfo = keyInfo;

            Reference reference = new Reference();

            reference.Id = "ReferenceKeyInfo";
            reference.Uri = "#KeyInfoId-" + _signatureId;

            _xadesSignedXml.AddReference(reference);
        }

        private HashAlgorithm ObtenerAlgoritmoHuella(TipoAlgoritmo tipo)
        {
            if (tipo == TipoAlgoritmo.SHA1)
            {
                return SHA1.Create();
            }
            else if (tipo == TipoAlgoritmo.SHA256)
            {
                return SHA256.Create();
            }
            else
            {
                return SHA512.Create();
            }
        }

        private void InsertarPropiedadesFirma(SignedSignatureProperties signedSignatureProperties, SignedDataObjectProperties signedDataObjectProperties,
                   UnsignedSignatureProperties unsignedSignatureProperties, string mimeType, X509Certificate2 certificado)
        {
            XmlDocument xmlDocument;
            Cert cert;

            xmlDocument = new XmlDocument();

            cert = new Cert();
            cert.IssuerSerial.X509IssuerName = certificado.IssuerName.Name;
            cert.IssuerSerial.X509SerialNumber = HexToDecimal(certificado.SerialNumber);

            using (var alg = ObtenerAlgoritmoHuella(_algoritmoReferencias))
            {
                cert.CertDigest.DigestMethod.Algorithm = _algoritmoRefUrl;
                cert.CertDigest.DigestValue = alg.ComputeHash(certificado.GetRawCertData());
            }

            signedSignatureProperties.SigningCertificate.CertCollection.Add(cert);

            if (!string.IsNullOrEmpty(_policyIdentifier))
            {
                signedSignatureProperties.SignaturePolicyIdentifier.SignaturePolicyImplied = false;
                signedSignatureProperties.SignaturePolicyIdentifier.SignaturePolicyId.SigPolicyId.Identifier.IdentifierUri = _policyIdentifier;
            }

            if (!string.IsNullOrEmpty(_policyUri))
            {
                SigPolicyQualifier spq = new SigPolicyQualifier();
                spq.AnyXmlElement = xmlDocument.CreateElement("SPURI", XadesSignedXml.XadesNamespaceUri);
                spq.AnyXmlElement.InnerText = _policyUri;

                signedSignatureProperties.SignaturePolicyIdentifier.SignaturePolicyId.SigPolicyQualifiers.SigPolicyQualifierCollection.Add(spq);
            }

            if (!string.IsNullOrEmpty(_policyHash))
            {
                signedSignatureProperties.SignaturePolicyIdentifier.SignaturePolicyId.SigPolicyHash.DigestMethod.Algorithm = SignedXml.XmlDsigSHA1Url;
                signedSignatureProperties.SignaturePolicyIdentifier.SignaturePolicyId.SigPolicyHash.DigestValue = Convert.FromBase64String(PolicyHash);
            }

            signedSignatureProperties.SigningTime = DateTime.Now;

            if (!string.IsNullOrEmpty(mimeType))
            {
                DataObjectFormat newDataObjectFormat = new DataObjectFormat();

                newDataObjectFormat.MimeType = mimeType;
                newDataObjectFormat.ObjectReferenceAttribute = "#" + _objectReference;

                signedDataObjectProperties.DataObjectFormatCollection.Add(newDataObjectFormat);
            }

        }


        private void ComputarFirma()
        {
            try
            {
                _xadesSignedXml.ComputeSignature();
                _xadesSignedXml.SignatureValueId = _signatureValueId;
            }
            catch (Exception exception)
            {
                throw new Exception("Ha ocurrido durante el proceso de firmado: " + exception.Message);
            }
        }

        #region Xades-T

        /// <summary>
        /// Amplia la firma actual a XADES-T.
        /// </summary>
        public void AmpliarAXadesT()
        {
            TimeStamp signatureTimeStamp;
            ArrayList signatureValueElementXpaths;
            byte[] signatureValueHash;

            try
            {
                signatureValueElementXpaths = new ArrayList();
                signatureValueElementXpaths.Add("ds:SignatureValue");
                signatureValueHash = XMLUtil.ComputeHashValueOfElementList(_xadesSignedXml, signatureValueElementXpaths);

                byte[] tsa = TimeStampClient.GetTimeStamp(_tsaServer, signatureValueHash, true);

                signatureTimeStamp = new TimeStamp("SignatureTimeStamp");
                signatureTimeStamp.Id = "SignatureTimeStamp-" + _xadesSignedXml.Signature.Id;
                signatureTimeStamp.EncapsulatedTimeStamp.PkiData = tsa;
                signatureTimeStamp.EncapsulatedTimeStamp.Id = "SignatureTimeStamp-" + Guid.NewGuid().ToString();

                UnsignedProperties unsignedProperties = _xadesSignedXml.UnsignedProperties;
                unsignedProperties.UnsignedSignatureProperties.SignatureTimeStampCollection.Add(signatureTimeStamp);

                _xadesSignedXml.UnsignedProperties = unsignedProperties;

                ActualizarDocumento();
            }
            catch (Exception ex)
            {
                throw new Exception("Ha ocurrido un error al insertar el sellado de tiempo.", ex);
            }

        }

        #endregion

        #region Xades-XL


        private string InvertirEmisor(string issuer)
        {
            string[] tokens = issuer.Split(',');
            string result = "";

            for (int i = tokens.Length - 1; i >= 0; i--)
            {
                if (!string.IsNullOrEmpty(result))
                {
                    result += ",";
                }

                result += tokens[i];
            }

            return result;
        }

        private bool StartEqual(string cert1, string cert2)
        {
            string[] tokens1 = cert1.Split(',');
            string[] tokens2 = cert2.Split(',');

            return tokens1[0] == tokens2[0];
        }

        private string HexToDecimal(string hex)
        {
            List<int> dec = new List<int> { 0 };

            foreach (char c in hex)
            {
                int carry = Convert.ToInt32(c.ToString(), 16);

                for (int i = 0; i < dec.Count; ++i)
                {
                    int val = dec[i] * 16 + carry;
                    dec[i] = val % 10;
                    carry = val / 10;
                }

                while (carry > 0)
                {
                    dec.Add(carry % 10);
                    carry /= 10;
                }
            }

            var chars = dec.Select(d => (char)('0' + d));
            var cArr = chars.Reverse().ToArray();
            return new string(cArr);
        }

        private string ObtenerResponderName(ResponderID responderId, ref bool byKey)
        {
            Org.BouncyCastle.Asn1.DerTaggedObject dt = (Org.BouncyCastle.Asn1.DerTaggedObject)responderId.ToAsn1Object();


            if (dt.TagNo == 1)
            {
                Org.BouncyCastle.Asn1.X509.X509Name name = Org.BouncyCastle.Asn1.X509.X509Name.GetInstance(dt.GetObject());
                byKey = false;

                return name.ToString();
            }
            else if (dt.TagNo == 2)
            {
                Asn1TaggedObject tagger = (Asn1TaggedObject)responderId.ToAsn1Object();
                Asn1OctetString pubInfo = (Asn1OctetString)tagger.GetObject();
                byKey = true;

                return Convert.ToBase64String(pubInfo.GetOctets());
            }
            else
            {
                return null;
            }
        }


        private void AddCertificates(X509Certificate2[] certs)
        {
            foreach (var cert in certs)
            {
                if (!_chain.ChainPolicy.ExtraStore.Contains(cert))
                {
                    _chain.ChainPolicy.ExtraStore.Add(cert);
                }
                else
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Inserta en la lista de certificados el certificado y comprueba la valided del certificado.
        /// </summary>
        /// <param name="cert"></param>
        /// <param name="unsignedProperties"></param>
        /// <param name="addCertValue"></param>
        private void InsertarCertificado(X509Certificate2 cert, UnsignedProperties unsignedProperties, bool addCert)
        {
            string digest = Convert.ToBase64String(cert.GetCertHash());

            if (_certificatesChecked.Contains(digest))
            {
                return;
            }

            if (addCert)
            {
                string guidCert = Guid.NewGuid().ToString();

                Cert chainCert = new Cert();
                chainCert.IssuerSerial.X509IssuerName = cert.IssuerName.Name;
                chainCert.IssuerSerial.X509SerialNumber = HexToDecimal(cert.SerialNumber);

                using (var alg = ObtenerAlgoritmoHuella(_algoritmoReferencias))
                {
                    chainCert.CertDigest.DigestMethod.Algorithm = _algoritmoRefUrl;
                    chainCert.CertDigest.DigestValue = alg.ComputeHash(cert.GetRawCertData());
                }
                
                chainCert.URI = "#Cert" + guidCert;
                unsignedProperties.UnsignedSignatureProperties.CompleteCertificateRefs.CertRefs.CertCollection.Add(chainCert);

                EncapsulatedX509Certificate encapsulatedX509Certificate = new EncapsulatedX509Certificate();
                encapsulatedX509Certificate.Id = "Cert" + guidCert;
                encapsulatedX509Certificate.PkiData = cert.GetRawCertData();
                unsignedProperties.UnsignedSignatureProperties.CertificateValues.EncapsulatedX509CertificateCollection.Add(encapsulatedX509Certificate);
            }

            _chain.Build(cert);

            var chains = _chain.ChainElements;

            if (chains.Count > 1)
            {
                X509ChainElementEnumerator enumerator = chains.GetEnumerator();
                enumerator.MoveNext(); // el mismo certificado que el pasado por parametro

                enumerator.MoveNext();

                var ocspCerts = ComprobarCertificado(unsignedProperties, cert, enumerator.Current.Certificate);

                _certificatesChecked.Add(digest);

                AddCertificates((from certOcsp in ocspCerts
                                 select new X509Certificate2(certOcsp.GetEncoded())).ToArray());

                foreach (var certOcsp in ocspCerts)
                {
                    InsertarCertificado(new X509Certificate2(certOcsp.GetEncoded()), unsignedProperties, true);
                }

                InsertarCertificado(enumerator.Current.Certificate, unsignedProperties, true);
            }
            else
            {
                var ocspCerts = ComprobarCertificado(unsignedProperties, cert, cert);

                _certificatesChecked.Add(digest);

                AddCertificates((from certOcsp in ocspCerts
                                 select new X509Certificate2(certOcsp.GetEncoded())).ToArray());

                foreach (var certOcsp in ocspCerts)
                {
                    InsertarCertificado(new X509Certificate2(certOcsp.GetEncoded()), unsignedProperties, true);
                }
            }

        }



        private Org.BouncyCastle.X509.X509Certificate[] ComprobarCertificado(UnsignedProperties unsignedProperties, X509Certificate2 client, X509Certificate2 issuer)
        {
            bool byKey = false;
            Org.BouncyCastle.X509.X509Certificate clientCert = new Org.BouncyCastle.X509.X509CertificateParser().ReadCertificate(client.RawData);
            Org.BouncyCastle.X509.X509Certificate issuerCert = new Org.BouncyCastle.X509.X509CertificateParser().ReadCertificate(issuer.RawData);

            OcspClient ocsp = new OcspClient();
            string ocspUrl = ocsp.GetAuthorityInformationAccessOcspUrl(issuerCert);

            if (string.IsNullOrEmpty(ocspUrl))
            {
                if (!string.IsNullOrEmpty(_ocspServer))
                {
                    ocspUrl = _ocspServer;
                }
                else
                {
                    throw new Exception("No se puede validar el certificado por OCSP.");
                }
            }

            byte[] resp = ocsp.QueryBinary(clientCert, issuerCert, ocspUrl);

            Org.BouncyCastle.Ocsp.OcspResp r = new OcspResp(resp);
            byte[] rEncoded = r.GetEncoded();
            BasicOcspResp or = (BasicOcspResp)r.GetResponseObject();

            string guidOcsp = Guid.NewGuid().ToString();

            OCSPRef ocspRef = new OCSPRef();
            ocspRef.OCSPIdentifier.UriAttribute = "#OcspValue" + guidOcsp;

            using (var alg = ObtenerAlgoritmoHuella(_algoritmoReferencias))
            {
                ocspRef.CertDigest.DigestMethod.Algorithm = _algoritmoRefUrl;
                ocspRef.CertDigest.DigestValue = alg.ComputeHash(rEncoded, 0, rEncoded.Length);
            }

            Org.BouncyCastle.Asn1.Ocsp.ResponderID rpId = or.ResponderId.ToAsn1Object();
            string name = ObtenerResponderName(rpId, ref byKey);

            if (!byKey)
            {
                ocspRef.OCSPIdentifier.ResponderID = name.ToString();

                if (!StartEqual(client.IssuerName.Name, ocspRef.OCSPIdentifier.ResponderID))
                {
                    ocspRef.OCSPIdentifier.ResponderID = InvertirEmisor(ocspRef.OCSPIdentifier.ResponderID);
                }
            }
            else
            {
                ocspRef.OCSPIdentifier.ResponderID = name;
                ocspRef.OCSPIdentifier.ByKey = true;
            }

            ocspRef.OCSPIdentifier.ProducedAt = or.ProducedAt;
            unsignedProperties.UnsignedSignatureProperties.CompleteRevocationRefs.OCSPRefs.OCSPRefCollection.Add(ocspRef);

            OCSPValue ocspValue = new OCSPValue();
            ocspValue.PkiData = rEncoded;
            ocspValue.Id = "OcspValue" + guidOcsp;
            unsignedProperties.UnsignedSignatureProperties.RevocationValues.OCSPValues.OCSPValueCollection.Add(ocspValue);

            CertificateStatus status = ocsp.ProcessOcspResponse(clientCert, issuerCert, resp);

            if (status != CertificateStatus.Good)
            {
                throw new Exception("El estado del certificado no es correcto.");
            }

            return or.GetCerts();

        }

        /// <summary>
        /// Inserta y valida los certificados del servidor de sellado de tiempo.
        /// </summary>
        /// <param name="unsignedProperties"></param>
        private void InsertarCertificadosTSA(UnsignedProperties unsignedProperties)
        {
            TimeStampToken token = new TimeStampToken(new Org.BouncyCastle.Cms.CmsSignedData(unsignedProperties.UnsignedSignatureProperties.SignatureTimeStampCollection[0].EncapsulatedTimeStamp.PkiData));
            IX509Store store = token.GetCertificates("Collection");

            Org.BouncyCastle.Cms.SignerID signerId = token.SignerID;

            var tsaCerts = store.GetMatches(null);

            List<X509Certificate2> certs = new List<X509Certificate2>();

            foreach (var item in tsaCerts)
            {
                Org.BouncyCastle.X509.X509Certificate cert = (Org.BouncyCastle.X509.X509Certificate)item;

                certs.Add(new X509Certificate2(cert.GetEncoded()));
            }

            AddCertificates(certs.ToArray());

            foreach (var item in certs)
            {
                InsertarCertificado(item, unsignedProperties, true);
            }
        }

        /// <summary>
        /// Amplia la firma actual a XADES-XL.
        /// </summary>
        public void AmpliarAXadesXL()
        {
            UnsignedProperties unsignedProperties = null;
            CertificateValues certificateValues = null;

            unsignedProperties = _xadesSignedXml.UnsignedProperties;
            unsignedProperties.UnsignedSignatureProperties.CompleteCertificateRefs = new CompleteCertificateRefs();
            unsignedProperties.UnsignedSignatureProperties.CompleteCertificateRefs.Id = "CompleteCertificates-" + Guid.NewGuid().ToString();

            unsignedProperties.UnsignedSignatureProperties.CertificateValues = new CertificateValues();
            certificateValues = unsignedProperties.UnsignedSignatureProperties.CertificateValues;
            certificateValues.Id = "CertificatesValues-" + Guid.NewGuid().ToString();

            unsignedProperties.UnsignedSignatureProperties.CompleteRevocationRefs = new CompleteRevocationRefs();
            unsignedProperties.UnsignedSignatureProperties.CompleteRevocationRefs.Id = "CompleteRev-" + Guid.NewGuid().ToString();

            unsignedProperties.UnsignedSignatureProperties.RevocationValues = new RevocationValues();
            unsignedProperties.UnsignedSignatureProperties.RevocationValues.Id = "RevocationValues-" + Guid.NewGuid().ToString();

            if (_certificatesChecked == null)
            {
                _certificatesChecked = new List<string>();
            }
            else
            {
                _certificatesChecked.Clear();
            }


            foreach (X509ChainElement element in _chain.ChainElements)
            {
                // el certificado de firma no se incluye en la lista de certificados, pero sí se valida.
                bool addCertValue = element.Certificate.SerialNumber != _certificate.SerialNumber;

                InsertarCertificado(element.Certificate, unsignedProperties, addCertValue);
            }

            InsertarCertificadosTSA(unsignedProperties);

            _xadesSignedXml.UnsignedProperties = unsignedProperties;

            SellarEstadosCertificados();

            ActualizarDocumento();
        }

        private void SellarEstadosCertificados()
        {
            TimeStamp xadesXTimeStamp;
            ArrayList signatureValueElementXpaths;
            byte[] signatureValueHash;

            XmlElement nodoFirma = _xadesSignedXml.GetSignatureElement();

            XmlNamespaceManager nm = new XmlNamespaceManager(_xmlDocument.NameTable);
            nm.AddNamespace("xades", XadesSignedXml.XadesNamespaceUri);
            nm.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);

            XmlNode xmlCompleteCertRefs = nodoFirma.SelectSingleNode("ds:Object/xades:QualifyingProperties/xades:UnsignedProperties/xades:UnsignedSignatureProperties/xades:CompleteCertificateRefs", nm);

            if (xmlCompleteCertRefs == null)
            {
                ActualizarDocumento();
            }

            signatureValueElementXpaths = new ArrayList();
            signatureValueElementXpaths.Add("ds:SignatureValue");
            signatureValueElementXpaths.Add("ds:Object/xades:QualifyingProperties/xades:UnsignedProperties/xades:UnsignedSignatureProperties/xades:SignatureTimeStamp");
            signatureValueElementXpaths.Add("ds:Object/xades:QualifyingProperties/xades:UnsignedProperties/xades:UnsignedSignatureProperties/xades:CompleteCertificateRefs");
            signatureValueElementXpaths.Add("ds:Object/xades:QualifyingProperties/xades:UnsignedProperties/xades:UnsignedSignatureProperties/xades:CompleteRevocationRefs");
            signatureValueHash = XMLUtil.ComputeHashValueOfElementList(_xadesSignedXml, signatureValueElementXpaths);

            byte[] tsa = TimeStampClient.GetTimeStamp(_tsaServer, signatureValueHash, true);

            xadesXTimeStamp = new TimeStamp("SigAndRefsTimeStamp");
            xadesXTimeStamp.Id = "SigAndRefsStamp-" + _xadesSignedXml.Signature.Id;
            xadesXTimeStamp.EncapsulatedTimeStamp.PkiData = tsa;
            xadesXTimeStamp.EncapsulatedTimeStamp.Id = "SigAndRefsStamp-" + Guid.NewGuid().ToString();
            UnsignedProperties unsignedProperties = _xadesSignedXml.UnsignedProperties;

            unsignedProperties.UnsignedSignatureProperties.RefsOnlyTimeStampFlag = false;
            unsignedProperties.UnsignedSignatureProperties.SigAndRefsTimeStampCollection.Add(xadesXTimeStamp);


            _xadesSignedXml.UnsignedProperties = unsignedProperties;
        }

        #endregion

        public void Dispose()
        {
            if (_liberarCryptoProvider && _rsaKey != null)
            {
                _rsaKey.Dispose();
            }
        }
    }
}
