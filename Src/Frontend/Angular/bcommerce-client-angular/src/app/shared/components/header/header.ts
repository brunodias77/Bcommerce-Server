import { Component, HostListener, signal } from '@angular/core';
import { UserIcon } from '../../icons/user-icon/user-icon';
import { HeartIcon } from '../../icons/heart-icon/heart-icon';
import { CartIcon } from '../../icons/cart-icon/cart-icon';

@Component({
  selector: 'app-header',
  imports: [UserIcon, HeartIcon, CartIcon],
  templateUrl: './header.html',
  styleUrl: './header.css',
})
export class Header {
  // Signal para controlar o estado do scroll
  isScrolled = signal(false);

  // Signal para controlar o estado do menu mobile
  isMobileMenuOpen = signal(false);

  @HostListener('window:scroll', [])
  onWindowScroll() {
    const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
    this.isScrolled.set(scrollTop > 50);
  }

  // Classes computadas - Estilo E-commerce
  headerClasses() {
    const base = 'fixed z-50 transition-all duration-300 ease-out shadow-md';
    return this.isScrolled()
      ? `${base} left-4 right-4 top-4 rounded-2xl bg-white/50 backdrop-blur-md`
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
    const base = 'relative flex items-center text-gray-600  transition-all duration-300 rounded-lg';
    return this.isScrolled() ? `${base} px-2 py-2 ` : `${base} px-3 py-2 `;
  }

  // Função para alternar o estado do menu mobile
  toggleMobileMenu() {
    this.isMobileMenuOpen.set(!this.isMobileMenuOpen());
  }

  // Função para fechar o menu mobile
  closeMobileMenu() {
    this.isMobileMenuOpen.set(false);
  }
}
