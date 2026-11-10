"use client";

import { useEffect, useRef, useState, type ComponentProps, type ReactNode } from "react";
import { Check, ChevronDown, ChevronLeft, ChevronRight, CircleCheck, Info, TriangleAlert } from "lucide-react";

// Componentes de módulo de Figma (página Alumnos, sección «Componentes de módulo»).

const tonos = {
  verde: "bg-portal-exito-fondo text-portal-exito-texto",
  azul: "bg-portal-activo-fondo text-portal-activo",
  ambar: "bg-portal-advertencia-fondo text-portal-advertencia-texto",
  naranja: "bg-portal-naranja-fondo text-portal-naranja-texto",
  rojo: "bg-portal-error-fondo text-portal-error-texto",
  gris: "bg-portal-neutro-fondo text-portal-subtexto",
};
export type Tono = keyof typeof tonos;

export function Insignia({ tono, children }: { tono: Tono; children: string }) {
  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full px-2 py-[3px] text-xs font-semibold whitespace-nowrap ${tonos[tono]}`}>
      <span className="size-1.5 rounded-full bg-current" aria-hidden />
      {children}
    </span>
  );
}

export function BarraAvance({ porcentaje }: { porcentaje: number }) {
  return (
    <span className="flex items-center gap-2">
      <span className="h-1.5 w-[72px] overflow-hidden rounded-[3px] bg-portal-neutro-fondo" role="progressbar" aria-valuenow={porcentaje} aria-valuemin={0} aria-valuemax={100}>
        <span className="block h-full bg-portal-activo" style={{ width: `${Math.min(porcentaje, 100)}%` }} />
      </span>
      <span className="text-[13px] font-medium text-portal-menu-texto">{Math.round(porcentaje)} %</span>
    </span>
  );
}

// Variantes de Figma «Botón portal»: principal (relleno), contorno y secundario (solo texto).
export function BotonPortal({ variante = "principal", className = "", ...boton }: ComponentProps<"button"> & { variante?: "principal" | "contorno" | "secundario" }) {
  const estilo = {
    principal: "bg-portal-activo text-blanco",
    contorno: "border border-portal-borde-campo bg-blanco text-portal-menu-texto",
    secundario: "text-portal-activo",
  }[variante];
  return <button className={`flex items-center gap-2 rounded-lg py-2.5 pr-[18px] pl-4 text-[15px] font-semibold disabled:opacity-50 ${estilo} ${className}`} {...boton} />;
}

// «espacio»: dentro de una tarjeta (consulta) o suelto en la página (ficha), según Figma.
export function Pestanas<T extends string>({ pestanas, actual, onCambio, espacio = "gap-6 px-5 pt-1.5" }: {
  pestanas: { valor: T; texto: string; conteo?: number }[];
  actual: T;
  onCambio: (valor: T) => void;
  espacio?: string;
}) {
  return (
    <div role="tablist" className={`flex border-b border-portal-borde ${espacio}`}>
      {pestanas.map((p) => {
        const activa = p.valor === actual;
        return (
          <button
            key={p.valor}
            role="tab"
            aria-selected={activa}
            onClick={() => onCambio(p.valor)}
            className={`flex items-center gap-2 border-b-2 px-1 py-2.5 text-[15px] ${activa ? "border-portal-activo font-semibold text-portal-activo" : "border-transparent font-medium text-portal-subtexto"}`}
          >
            {p.texto}
            {p.conteo !== undefined && (
              <span className={`rounded-full px-[7px] py-px text-xs font-semibold ${activa ? "bg-portal-activo-fondo" : "bg-portal-neutro-fondo"}`}>{p.conteo}</span>
            )}
          </button>
        );
      })}
    </div>
  );
}

// Tarjeta de la ficha (Figma F1): título, insignia opcional a la derecha y contenido.
export function TarjetaPortal({ titulo, insignia, children }: { titulo: string; insignia?: ReactNode; children: ReactNode }) {
  return (
    <section className="flex flex-1 flex-col gap-3.5 rounded-xl border border-portal-borde bg-blanco px-[22px] py-5">
      <div className="flex items-center justify-between">
        <h2 className="font-semibold text-portal-titulo">{titulo}</h2>
        {insignia}
      </div>
      {children}
    </section>
  );
}

// Renglón «etiqueta: valor» de las tarjetas de la ficha.
export function Dato({ etiqueta, children }: { etiqueta: string; children: ReactNode }) {
  return (
    <div className="flex items-center gap-3 text-sm">
      <dt className="w-[170px] shrink-0 text-portal-subtexto">{etiqueta}</dt>
      <dd className="font-medium text-portal-titulo">{children}</dd>
    </div>
  );
}

export function Casilla({ marcada }: { marcada: boolean }) {
  return marcada ? (
    <span className="flex size-[18px] items-center justify-center rounded-[5px] bg-portal-activo text-blanco" aria-hidden>
      <Check size={14} strokeWidth={3} />
    </span>
  ) : (
    <span className="size-[18px] rounded-[5px] border-[1.5px] border-portal-borde-campo bg-blanco" aria-hidden />
  );
}

// Abre y cierra un desplegable: se cierra al hacer clic fuera o con Escape, y se abre hacia la
// izquierda si hacia la derecha no cabe en la pantalla.
const ANCHO_DESPLEGABLE = 270;
export function useDesplegable() {
  const [abierto, setAbierto] = useState(false);
  const [lado, setLado] = useState("left-0");
  const caja = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!abierto) return;
    const fuera = (e: PointerEvent) => !caja.current?.contains(e.target as Node) && setAbierto(false);
    const escape = (e: KeyboardEvent) => e.key === "Escape" && setAbierto(false);
    document.addEventListener("pointerdown", fuera);
    document.addEventListener("keydown", escape);
    return () => {
      document.removeEventListener("pointerdown", fuera);
      document.removeEventListener("keydown", escape);
    };
  }, [abierto]);

  const alternar = () => {
    const izquierda = caja.current?.getBoundingClientRect().left ?? 0;
    setLado(izquierda + ANCHO_DESPLEGABLE > window.innerWidth - 16 ? "right-0" : "left-0");
    setAbierto(!abierto);
  };
  return { caja, abierto, setAbierto, alternar, lado };
}

// Píldora de filtro con su desplegable de opción múltiple (Figma A2, «Popover · Titulación»).
export function FiltroMultiple({ etiqueta, titulo, opciones, seleccion, onAplicar }: {
  etiqueta: string;
  titulo: string;
  opciones: { valor: string; conteo: number }[];
  seleccion: string[];
  onAplicar: (valores: string[]) => void;
}) {
  const { caja, abierto, setAbierto, alternar, lado } = useDesplegable();
  const [borrador, setBorrador] = useState<string[]>([]);

  const activo = seleccion.length > 0;
  const texto = !activo ? etiqueta : seleccion.length === 1 ? `${etiqueta}: ${seleccion[0]}` : `${etiqueta}: ${seleccion.length}`;
  const aplicar = (valores: string[]) => {
    onAplicar(valores);
    setAbierto(false);
  };

  return (
    <div ref={caja} className="relative">
      <button
        aria-expanded={abierto}
        onClick={() => {
          setBorrador(seleccion);
          alternar();
        }}
        className={`flex items-center gap-1.5 rounded-full border py-2 pr-2.5 pl-3.5 text-sm ${activo ? "border-portal-activo bg-portal-activo-fondo font-semibold text-portal-activo" : "border-portal-borde-campo bg-blanco font-medium text-portal-menu-texto"}`}
      >
        {texto}
        <ChevronDown size={16} aria-hidden />
      </button>
      {abierto && (
        <fieldset className={`absolute top-full ${lado} z-10 mt-2 flex w-[270px] flex-col gap-1 rounded-[10px] border border-portal-borde bg-blanco p-3 shadow-[0_8px_24px_rgba(10,18,51,0.14)]`}>
          <legend className="contents text-xs font-semibold text-portal-subtexto uppercase">{titulo}</legend>
          {opciones.map((o) => {
            const marcada = borrador.includes(o.valor);
            return (
              <label key={o.valor} className="flex cursor-pointer items-center gap-2.5 rounded-md px-1.5 py-2 hover:bg-portal-tabla-cabecera">
                <input
                  type="checkbox"
                  className="sr-only"
                  checked={marcada}
                  onChange={() => setBorrador(marcada ? borrador.filter((v) => v !== o.valor) : [...borrador, o.valor])}
                />
                <Casilla marcada={marcada} />
                <span className={`flex-1 text-sm text-portal-menu-texto ${marcada ? "font-medium" : ""}`}>{o.valor}</span>
                <span className="text-[13px] text-portal-placeholder">{o.conteo}</span>
              </label>
            );
          })}
          <div className="flex items-center justify-between border-t border-portal-borde pt-2">
            <BotonPortal variante="secundario" onClick={() => aplicar([])}>Limpiar</BotonPortal>
            <BotonPortal onClick={() => aplicar(borrador)}>Aplicar</BotonPortal>
          </div>
        </fieldset>
      )}
    </div>
  );
}

// Alerta del portal (Figma, componente «Alerta» 13:535 y «Aviso del portal» 8:557): con ícono.
const alertas = {
  advertencia: { icono: TriangleAlert, estilo: "border-portal-advertencia-borde bg-portal-advertencia-fondo text-portal-advertencia-texto" },
  error: { icono: TriangleAlert, estilo: "border-portal-error-texto/25 bg-portal-error-fondo text-portal-error-texto" },
  exito: { icono: CircleCheck, estilo: "border-portal-exito-borde bg-portal-exito-fondo text-portal-exito-texto" },
  info: { icono: Info, estilo: "border-portal-info-borde bg-portal-info-fondo text-portal-activo" },
};

export function Alerta({ tipo, children }: { tipo: keyof typeof alertas; children: ReactNode }) {
  const { icono: Icono, estilo } = alertas[tipo];
  return (
    <div
      role={tipo === "advertencia" || tipo === "error" ? "alert" : "status"}
      className={`flex items-start gap-3 rounded-lg border px-4 py-3.5 text-sm ${estilo}`}
    >
      <Icono size={20} className="shrink-0" aria-hidden />
      <div className="flex-1">{children}</div>
    </div>
  );
}

// Pasos de un asistente (Figma, componente «Paso» 13:511): completos en verde, el actual en azul.
export function Pasos({ pasos, actual }: { pasos: string[]; actual: number }) {
  return (
    <ol className="flex items-center gap-3.5">
      {pasos.map((texto, i) => {
        const estado = i < actual ? "completo" : i === actual ? "actual" : "pendiente";
        return (
          <li key={texto} className={`flex items-center gap-3.5 ${i < pasos.length - 1 ? "flex-1" : ""}`} aria-current={estado === "actual" ? "step" : undefined}>
            <span className="flex shrink-0 items-center gap-2.5">
              <span
                className={`flex size-[30px] items-center justify-center rounded-full text-sm font-bold ${estado === "completo" ? "bg-portal-exito-texto text-blanco" : estado === "actual" ? "bg-portal-activo text-blanco" : "border-[1.5px] border-portal-borde-campo bg-blanco text-portal-subtexto"}`}
              >
                {estado === "completo" ? <Check size={16} strokeWidth={3} aria-label="Completo" /> : i + 1}
              </span>
              <span className={`text-[15px] ${estado === "actual" ? "font-semibold text-portal-titulo" : estado === "completo" ? "font-medium text-portal-titulo" : "font-medium text-portal-subtexto"}`}>{texto}</span>
            </span>
            {i < pasos.length - 1 && <span className={`h-0.5 flex-1 ${i < actual ? "bg-portal-exito-texto" : "bg-portal-borde-campo"}`} aria-hidden />}
          </li>
        );
      })}
    </ol>
  );
}

// Diálogo modal (Figma F5-F7) sobre <dialog>: fondo, foco atrapado y Escape los da el navegador.
// El contenido solo existe mientras está abierto, así cada apertura empieza con el formulario limpio.
export function Dialogo({ abierto, onCerrar, titulo, subtitulo, ancho, children }: {
  abierto: boolean;
  onCerrar: () => void;
  titulo: string;
  subtitulo?: string;
  ancho: string;
  children: ReactNode;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    if (abierto) ref.current?.showModal();
    else ref.current?.close();
  }, [abierto]);
  return (
    <dialog
      ref={ref}
      onClose={onCerrar}
      aria-labelledby="dialogo-titulo"
      className={`m-auto rounded-[14px] bg-blanco px-7 pt-7 pb-6 shadow-[0_16px_40px_rgba(8,15,41,0.25)] backdrop:bg-[rgba(8,15,41,0.35)] ${ancho}`}
    >
      {abierto && (
        <div className="flex flex-col gap-5">
          <div className="flex flex-col gap-1">
            <h2 id="dialogo-titulo" className="text-xl font-bold text-portal-titulo">{titulo}</h2>
            {subtitulo && <p className="text-sm text-portal-descripcion">{subtitulo}</p>}
          </div>
          {children}
        </div>
      )}
    </dialog>
  );
}

const cajaCampo = "w-full rounded-lg border border-portal-borde-campo bg-blanco px-3 py-2.5 text-[15px] text-portal-titulo outline-none focus:border-portal-activo";

export function CampoPortal({ etiqueta, ...input }: { etiqueta: string } & ComponentProps<"input">) {
  return (
    <label className="flex flex-col gap-1.5">
      <span className="text-sm font-semibold text-portal-menu-texto">{etiqueta}</span>
      <input className={cajaCampo} {...input} />
    </label>
  );
}

// Área de texto (Figma 31:841).
export function AreaTexto({ etiqueta, ...area }: { etiqueta: string } & ComponentProps<"textarea">) {
  return (
    <label className="flex flex-col gap-1.5">
      <span className="text-sm font-semibold text-portal-menu-texto">{etiqueta}</span>
      <textarea className={`${cajaCampo} h-[110px] resize-none`} {...area} />
    </label>
  );
}

export function SelectorPortal({ etiqueta, children, ...select }: { etiqueta: string } & ComponentProps<"select">) {
  return (
    <label className="flex flex-col gap-1.5">
      <span className="text-sm font-semibold text-portal-menu-texto">{etiqueta}</span>
      <span className="relative">
        {/* Sin elegir (opción de valor vacío), el texto va en gris como el estado «Vacío» de Figma. */}
        <select className={`${cajaCampo} h-[42px] appearance-none pr-9 has-[option[value='']:checked]:text-portal-placeholder [&_option]:text-portal-titulo`} {...select}>{children}</select>
        <ChevronDown size={18} className="pointer-events-none absolute top-1/2 right-3 -translate-y-1/2 text-portal-subtexto" aria-hidden />
      </span>
    </label>
  );
}

// Píldora con desplegable para elegir un rango (mínimo y máximo) con un control de dos puntos.
// Son dos input range nativos encimados: el riel ignora el puntero y solo los puntos lo reciben.
export function FiltroRango({ etiqueta, titulo, unidad, min, max, paso, rango, onAplicar }: {
  etiqueta: string;
  titulo: string;
  unidad: string;
  min: number;
  max: number;
  paso: number;
  rango: [number, number] | null;
  onAplicar: (rango: [number, number] | null) => void;
}) {
  const { caja, abierto, setAbierto, alternar, lado } = useDesplegable();
  const [borrador, setBorrador] = useState<[number, number]>([min, max]);

  const [desde, hasta] = borrador;
  const texto = rango ? `${etiqueta}: ${rango[0]}–${rango[1]} ${unidad}` : etiqueta;
  const aplicar = (r: [number, number] | null) => {
    onAplicar(r);
    setAbierto(false);
  };
  const punto =
    "pointer-events-none absolute inset-0 w-full appearance-none bg-transparent [&::-webkit-slider-thumb]:pointer-events-auto [&::-webkit-slider-thumb]:size-[18px] [&::-webkit-slider-thumb]:cursor-pointer [&::-webkit-slider-thumb]:appearance-none [&::-webkit-slider-thumb]:rounded-full [&::-webkit-slider-thumb]:border-2 [&::-webkit-slider-thumb]:border-blanco [&::-webkit-slider-thumb]:bg-portal-activo [&::-webkit-slider-thumb]:shadow [&::-moz-range-thumb]:pointer-events-auto [&::-moz-range-thumb]:size-[18px] [&::-moz-range-thumb]:cursor-pointer [&::-moz-range-thumb]:rounded-full [&::-moz-range-thumb]:border-2 [&::-moz-range-thumb]:border-blanco [&::-moz-range-thumb]:bg-portal-activo";

  return (
    <div ref={caja} className="relative">
      <button
        aria-expanded={abierto}
        onClick={() => {
          setBorrador(rango ?? [min, max]);
          alternar();
        }}
        className={`flex items-center gap-1.5 rounded-full border py-2 pr-2.5 pl-3.5 text-sm ${rango ? "border-portal-activo bg-portal-activo-fondo font-semibold text-portal-activo" : "border-portal-borde-campo bg-blanco font-medium text-portal-menu-texto"}`}
      >
        {texto}
        <ChevronDown size={16} aria-hidden />
      </button>
      {abierto && (
        <fieldset className={`absolute top-full ${lado} z-10 mt-2 flex w-[270px] flex-col gap-3 rounded-[10px] border border-portal-borde bg-blanco p-3 shadow-[0_8px_24px_rgba(10,18,51,0.14)]`}>
          <legend className="contents text-xs font-semibold text-portal-subtexto uppercase">{titulo}</legend>
          <p className="px-1.5 text-sm font-medium text-portal-menu-texto">
            {desde} {unidad} – {hasta} {unidad}
          </p>
          <div className="relative mx-1.5 h-[18px]">
            <span className="absolute top-1/2 right-0 left-0 h-1.5 -translate-y-1/2 rounded-[3px] bg-portal-neutro-fondo" aria-hidden />
            <span
              className="absolute top-1/2 h-1.5 -translate-y-1/2 rounded-[3px] bg-portal-activo"
              style={{ left: `${((desde - min) / (max - min)) * 100}%`, right: `${100 - ((hasta - min) / (max - min)) * 100}%` }}
              aria-hidden
            />
            <input type="range" min={min} max={max} step={paso} value={desde} aria-label={`${etiqueta} mínimo`} className={punto}
              onChange={(e) => setBorrador([Math.min(Number(e.target.value), hasta), hasta])} />
            <input type="range" min={min} max={max} step={paso} value={hasta} aria-label={`${etiqueta} máximo`} className={punto}
              onChange={(e) => setBorrador([desde, Math.max(Number(e.target.value), desde)])} />
          </div>
          <div className="flex items-center justify-between border-t border-portal-borde pt-2">
            <BotonPortal variante="secundario" onClick={() => aplicar(null)}>Limpiar</BotonPortal>
            <BotonPortal onClick={() => aplicar(desde === min && hasta === max ? null : borrador)}>Aplicar</BotonPortal>
          </div>
        </fieldset>
      )}
    </div>
  );
}

// Páginas a mostrar: primera, última y la actual con sus vecinas; «…» en los huecos.
function paginasVisibles(actual: number, total: number): (number | "…")[] {
  const set = [...new Set([1, actual - 1, actual, actual + 1, total])].filter((p) => p >= 1 && p <= total).sort((a, b) => a - b);
  return set.flatMap((p, i) => (i > 0 && p - set[i - 1] > 1 ? ["…" as const, p] : [p]));
}

export function Paginacion({ actual, total, onCambio }: { actual: number; total: number; onCambio: (pagina: number) => void }) {
  const forma = "flex size-[34px] items-center justify-center rounded-lg border text-sm font-medium disabled:opacity-50";
  const caja = `${forma} border-portal-borde-campo bg-blanco text-portal-menu-texto`;
  return (
    <nav aria-label="Paginación" className="flex items-center gap-1.5">
      <button className={caja} disabled={actual === 1} onClick={() => onCambio(actual - 1)} aria-label="Página anterior">
        <ChevronLeft size={16} />
      </button>
      {paginasVisibles(actual, total).map((p, i) =>
        p === "…" ? (
          <span key={`h${i}`} className={caja}>…</span>
        ) : (
          <button
            key={p}
            onClick={() => onCambio(p)}
            aria-current={p === actual ? "page" : undefined}
            className={p === actual ? `${forma} border-portal-activo bg-portal-activo text-blanco` : caja}
          >
            {p}
          </button>
        ),
      )}
      <button className={caja} disabled={actual === total} onClick={() => onCambio(actual + 1)} aria-label="Página siguiente">
        <ChevronRight size={16} />
      </button>
    </nav>
  );
}
