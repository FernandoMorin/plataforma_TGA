"use client";

import { useEffect, useState, type FormEvent } from "react";
import { ChevronDown } from "lucide-react";
import { EncabezadoPagina } from "@/components/portal";
import { Alerta, BotonPortal, useDesplegable } from "@/components/modulo";
import { ListaAvances, type Avance } from "@/components/informe";
import { enviarFormulario, pdfInvalido, ZonaCarga } from "@/components/carga";
import { sesionVencida } from "@/lib/acceso";

// Avances del alumno (Figma AT6, renombrada): sube avances y anexos para el informe abierto o el siguiente,
// y ve lo que subió antes agrupado por informe.

type Datos = {
  puedeSubir: boolean;
  informe: number | null;
  evidencias: Avance[];
};

// Ids de CAT_tipo_documento (ver TipoDocumento en el backend).
const tipos = [
  { id: 1, nombre: "Avance", detalle: "Documento para el siguiente informe" },
  { id: 2, nombre: "Evidencia", detalle: "Anexos y material de apoyo" },
  { id: 5, nombre: "Trabajo final", detalle: "Tesis o artículo terminado" },
];

export function Avances() {
  const [datos, setDatos] = useState<Datos | null>(null);
  const [version, setVersion] = useState(0);

  useEffect(() => {
    fetch("/api/mi-titulacion/evidencias").then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setDatos(await res.json());
    });
  }, [version]);

  // Una tarjeta por informe, del más reciente al más antiguo (la lista ya viene de la más nueva a la más vieja).
  const porInforme = new Map<number, Datos["evidencias"]>();
  for (const d of datos?.evidencias ?? []) porInforme.set(d.informe, [...(porInforme.get(d.informe) ?? []), d]);
  const informes = [...porInforme.entries()].sort(([a], [b]) => b - a);

  return (
    <>
      <EncabezadoPagina titulo="Avances" descripcion="Sube tus avances para el siguiente informe." />
      {datos && !datos.informe && <p className="text-[15px] text-portal-subtexto">Podrás subir tus avances cuando tu director acepte tu solicitud.</p>}
      {datos?.puedeSubir && <Subir informe={datos.informe!} onListo={() => setVersion((v) => v + 1)} />}
      {informes.map(([numero, docs]) => (
        <section key={numero} className="mx-auto flex w-[720px] flex-col gap-4 rounded-xl border border-portal-borde bg-blanco px-6 py-[22px]">
          <h2 className="text-[17px] font-semibold text-portal-titulo">Informe {numero}</h2>
          <ListaAvances avances={docs} />
        </section>
      ))}
    </>
  );
}

function Subir({ informe, onListo }: { informe: number; onListo: () => void }) {
  const [tipo, setTipo] = useState(tipos[0]);
  const [archivo, setArchivo] = useState<File | null>(null);
  const [aviso, setAviso] = useState<{ tipo: "exito" | "error"; texto: string } | null>(null);
  const [enviando, setEnviando] = useState(false);
  const { caja, abierto, setAbierto, alternar } = useDesplegable();

  function elegir(f: File | undefined) {
    setAviso(null);
    if (!f) return setArchivo(null);
    const invalido = pdfInvalido(f);
    if (invalido) return setAviso({ tipo: "error", texto: invalido });
    setArchivo(f);
  }

  async function subir(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!archivo) return setAviso({ tipo: "error", texto: "Selecciona un archivo PDF." });
    const datos = new FormData();
    datos.set("tipo", String(tipo.id));
    datos.set("archivo", archivo);
    setEnviando(true);
    const fallo = await enviarFormulario("/api/mi-titulacion/evidencias", datos);
    setEnviando(false);
    if (fallo) return setAviso({ tipo: "error", texto: fallo });
    setAviso({ tipo: "exito", texto: `Se subió ${archivo.name}.` });
    setArchivo(null);
    onListo();
  }

  return (
    <form onSubmit={subir} className="mx-auto flex w-[720px] flex-col gap-4 rounded-xl border border-portal-borde bg-blanco px-6 py-[22px]">
      <div className="flex items-center justify-between">
        <h2 className="text-[17px] font-semibold text-portal-titulo">Subir evidencia</h2>
        <p className="text-[13px] font-medium text-portal-subtexto">Para el Informe {informe}</p>
      </div>
      <div ref={caja} className="relative flex w-[240px] flex-col gap-1.5">
        <span id="tipo-evidencia" className="text-sm font-semibold text-portal-menu-texto">Tipo</span>
        <button type="button" onClick={alternar} aria-haspopup="listbox" aria-expanded={abierto} aria-labelledby="tipo-evidencia"
          className="flex h-[42px] items-center gap-2 rounded-lg border border-portal-borde-campo bg-blanco px-3 text-left text-[15px] text-portal-titulo">
          <span className="flex-1">{tipo.nombre}</span>
          <ChevronDown size={18} className="text-portal-subtexto" aria-hidden />
        </button>
        {abierto && (
          <ul role="listbox" aria-labelledby="tipo-evidencia" className="absolute top-full z-10 mt-1.5 flex w-max min-w-full flex-col gap-0.5 rounded-[10px] border border-portal-borde bg-blanco p-1.5 shadow-[0_8px_24px_rgba(10,18,51,0.14)]">
            {tipos.map((t) => {
              const elegido = t.id === tipo.id;
              return (
                <li key={t.id} role="option" aria-selected={elegido}>
                  <button type="button" onClick={() => { setTipo(t); setAbierto(false); }}
                    className={`flex w-full flex-col gap-0.5 rounded-md px-2.5 py-2 text-left ${elegido ? "bg-portal-activo-fondo" : "hover:bg-portal-tabla-cabecera"}`}>
                    <span className={`text-sm font-semibold ${elegido ? "text-portal-activo" : "text-portal-titulo"}`}>{t.nombre}</span>
                    <span className="text-xs text-portal-subtexto">{t.detalle}</span>
                  </button>
                </li>
              );
            })}
          </ul>
        )}
      </div>
      <ZonaCarga archivo={archivo} onElegir={elegir} />
      {aviso && <Alerta tipo={aviso.tipo}>{aviso.texto}</Alerta>}
      <div className="flex justify-end">
        <BotonPortal type="submit" disabled={enviando}>Subir</BotonPortal>
      </div>
    </form>
  );
}
