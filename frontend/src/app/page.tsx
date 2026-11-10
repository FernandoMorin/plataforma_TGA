import { Suspense } from "react";
import { BarraDeFrase, Encabezado, PanelInformativo, PieDePagina } from "@/components/institucional";
import { Tarjeta } from "@/components/ui";
import { FormLogin } from "./form-login";

// Figma, página Login: 01 Normal, 02 Error de credenciales, 03 Cuenta bloqueada, 04 Sesión terminada.
export default function Login() {
  return (
    <>
      <Encabezado titulo="Inicio de Sesión Institucional" />
      <BarraDeFrase />
      <main className="flex-1 px-[92px]">
        <div className="bg-blanco flex flex-wrap items-start gap-[23px] px-[120px] py-[46px]">
          <Tarjeta titulo="Portal de estudiantes" className="w-[412px]">
            <Suspense>
              <FormLogin />
            </Suspense>
          </Tarjeta>
          <PanelInformativo />
        </div>
      </main>
      <PieDePagina />
    </>
  );
}
