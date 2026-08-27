import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "ContentHub — İçerik Arama",
  description: "Çoklu sağlayıcılı içerik arama ve puanlama arayüzü.",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="tr">
      <body>{children}</body>
    </html>
  );
}
