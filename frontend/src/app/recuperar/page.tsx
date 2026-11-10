import { BarraDeFrase, Encabezado, PanelInformativo, PieDePagina } from "@/components/institucional";
import { Tarjeta } from "@/components/ui";
import { Recuperar } from "./recuperar";

// Recuperar acceso (Figma, página Login, 05 y 06).
export default function PaginaRecuperar() {
  return (
    <>
      <Encabezado titulo="Inicio de Sesión Institucional" />
      <BarraDeFrase />
      <main className="flex-1 px-[92px]">
        <div className="bg-blanco flex flex-wrap items-start gap-[23px] px-[120px] py-[46px]">
          <Tarjeta titulo="Recuperar acceso" className="w-[412px]">
            <Recuperar />
          </Tarjeta>
          <PanelInformativo />
        </div>
      </main>
      <PieDePagina />
    </>
  );
}
