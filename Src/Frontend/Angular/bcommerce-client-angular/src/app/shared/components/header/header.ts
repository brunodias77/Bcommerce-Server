import { Component, HostListener, signal } from '@angular/core';
import { UserIcon } from '../../icons/user-icon/user-icon';
import { HeartIcon } from '../../icons/heart-icon/heart-icon';

@Component({
  selector: 'app-header',
  imports: [UserIcon, HeartIcon],
  templateUrl: './header.html',
  styleUrl: './header.css',
})
export class Header {
  // Signal para controlar o estado do scroll
  isScrolled = signal(false);

  // Dados de exemplo para produtos
  contentItems = [
    {
      id: 1,
      title: 'Camiseta Premium',
      description: 'Camiseta 100% algodão com design exclusivo e alta qualidade.',
      price: 89.9,
    },
    {
      id: 2,
      title: 'Tênis Esportivo',
      description: 'Tênis confortável e moderno para todas as ocasiões.',
      price: 249.9,
    },
    {
      id: 3,
      title: 'Jaqueta Jeans',
      description: 'Jaqueta clássica em jeans premium com acabamento impecável.',
      price: 199.9,
    },
    {
      id: 4,
      title: 'Relógio Digital',
      description: 'Relógio moderno com múltiplas funcionalidades e design elegante.',
      price: 159.9,
    },
    {
      id: 5,
      title: 'Mochila Urbana',
      description: 'Mochila versátil com compartimentos organizados e material resistente.',
      price: 129.9,
    },
    {
      id: 6,
      title: 'Óculos de Sol',
      description: 'Óculos com proteção UV e design contemporâneo.',
      price: 179.9,
    },
  ];

  scrollContent = [
    'Aproveite nossas ofertas especiais! Descontos de até 50% em produtos selecionados.',
    'Frete grátis para compras acima de R$ 199. Entrega rápida e segura em todo Brasil.',
    'Novidades toda semana! Cadastre-se e receba em primeira mão nossas promoções.',
    'Pagamento facilitado em até 12x sem juros. Aceitamos todos os cartões.',
    'Satisfação garantida ou seu dinheiro de volta. 30 dias para trocas e devoluções.',
  ];

  @HostListener('window:scroll', [])
  onWindowScroll() {
    const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
    this.isScrolled.set(scrollTop > 50);
  }

  // Classes computadas - Estilo E-commerce
  headerClasses() {
    const base = 'fixed z-50 transition-all duration-300 ease-out shadow-md';
    return this.isScrolled()
      ? `${base} left-4 right-4 top-4 rounded-2xl bg-white/30 backdrop-blur-md`
      : `${base} left-0 right-0 top-0 rounded-none bg-white`;
  }

  containerClasses() {
    const base = 'flex items-center justify-between mx-auto transition-all duration-300';
    return this.isScrolled()
      ? `${base} max-w-7xl px-4 py-2`
      : `${base} max-w-7xl px-4 sm:px-6 lg:px-8 py-4`;
  }

  mainClasses() {
    return this.isScrolled() ? 'pt-28' : 'pt-32';
  }

  logoClasses() {
    return this.isScrolled()
      ? 'w-7 h-7 transition-all duration-300'
      : 'w-9 h-9 transition-all duration-300';
  }

  titleClasses() {
    const base = 'font-bold text-gray-900 transition-all duration-300';
    return this.isScrolled() ? `${base} text-lg` : `${base} text-xl`;
  }

  navLinkClasses() {
    const base =
      'text-gray-600 hover:text-blue-600 transition-colors font-medium whitespace-nowrap';
    return this.isScrolled() ? `${base} text-sm` : `${base} text-sm`;
  }

  searchBarClasses() {
    const base =
      'flex items-center space-x-2 bg-gray-100 rounded-lg px-4 transition-all duration-300';
    return this.isScrolled() ? `${base} py-2` : `${base} py-2.5`;
  }

  searchInputClasses() {
    return 'flex-1 bg-transparent outline-none text-sm text-gray-700 placeholder-gray-400';
  }

  cartButtonClasses() {
    const base =
      'relative flex items-center text-gray-600 hover:text-blue-600 transition-all duration-300 rounded-lg';
    return this.isScrolled()
      ? `${base} px-3 py-2 hover:bg-blue-50`
      : `${base} px-4 py-2 hover:bg-blue-50`;
  }
}
