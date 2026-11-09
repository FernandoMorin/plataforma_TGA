"use client";

import { useEffect, useRef, useState, type ComponentProps } from "react";
import { Check, ChevronDown, ChevronLeft, ChevronRight } from "lucide-react";

// Componentes de módulo de Figma (página Alumnos, sección «Componentes de módulo»).

const tonos = {
  verde: "bg-portal-exito-fondo text-portal-exito-texto",
  azul: "bg-portal-activo-fondo text-portal-activo",
  ambar: "bg-portal-advertencia-fondo text-portal-advertencia-texto",
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

export function BotonPortal({ secundario, className = "", ...boton }: ComponentProps<"button"> & { secundario?: boolean }) {
  const estilo = secundario ? "text-portal-activo" : "bg-portal-activo text-blanco";
  return <button className={`flex items-center gap-2 rounded-lg py-2.5 pr-[18px] pl-4 text-[15px] font-semibold ${estilo} ${className}`} {...boton} />;
}

export function Pestanas<T extends string>({ pestanas, actual, onCambio }: {
  pestanas: { valor: T; texto: string; conteo: number }[];
  actual: T;
  onCambio: (valor: T) => void;
}) {
  return (
    <div role="tablist" className="flex gap-6 border-b border-portal-borde px-5 pt-1.5">
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
            <span className={`rounded-full px-[7px] py-px text-xs font-semibold ${activa ? "bg-portal-activo-fondo" : "bg-portal-neutro-fondo"}`}>{p.conteo}</span>
          </button>
        );
      })}
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
function useDesplegable() {
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
            <BotonPortal secundario onClick={() => aplicar([])}>Limpiar</BotonPortal>
            <BotonPortal onClick={() => aplicar(borrador)}>Aplicar</BotonPortal>
          </div>
        </fieldset>
      )}
    </div>
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
            <BotonPortal secundario onClick={() => aplicar(null)}>Limpiar</BotonPortal>
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
