using ECommerceAfternoon.Server.DTOs.Cart;
using ECommerceAfternoon.Server.Entities;
using ECommerceAfternoon.Server.Models;
using ECommerceAfternoon.Server.Repository.Abstract;
using ECommerceAfternoon.Server.Services.Concrete;
using Moq;

namespace WebApp.Tests.Services
{
    [TestFixture]
    public class CartServiceTests
    {
        private Mock<ICartRepository> _cartRepoMock = null!;
        private CartService _cartService = null!;

        [SetUp]
        public void SetUp()
        {
            _cartRepoMock = new Mock<ICartRepository>();
            _cartService = new CartService(_cartRepoMock.Object);
        }

        #region GetCartAsync

        [Test]
        public async Task GetCartAsync_WhenCartDoesNotExist_ReturnsEmptyCartDto()
        {
            // Arrange
            int userId = 1;
            _cartRepoMock.Setup(r => r.GetCartByUserIdAsync(userId))
                         .ReturnsAsync((Cart?)null);

            // Act
            var result = await _cartService.GetCartAsync(userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(0));
            Assert.That(result.UserId, Is.EqualTo(userId));
            Assert.That(result.Items, Is.Empty);
            Assert.That(result.Total, Is.EqualTo(0));
        }

        [Test]
        public async Task GetCartAsync_WhenCartExists_ReturnsMappedCartDtoWithCorrectTotal()
        {
            // Arrange
            int userId = 1;
            var cart = new Cart
            {
                Id = 10,
                UserId = userId,
                Items = new List<CartItem>
                {
                    new CartItem
                    {
                        Id = 1, ProductId = 100, Quantity = 2,
                        Product = new Product { Name = "iPhone 15", Price = 1000, ImageUrl = "iphone.png" }
                    },
                    new CartItem
                    {
                        Id = 2, ProductId = 101, Quantity = 1,
                        Product = new Product { Name = "AirPods", Price = 200, ImageUrl = "airpods.png" }
                    }
                }
            };

            _cartRepoMock.Setup(r => r.GetCartByUserIdAsync(userId))
                         .ReturnsAsync(cart);

            // Act
            var result = await _cartService.GetCartAsync(userId);

            // Assert
            Assert.That(result.Id, Is.EqualTo(10));
            Assert.That(result.UserId, Is.EqualTo(userId));
            Assert.That(result.Items.Count, Is.EqualTo(2));
            Assert.That(result.Total, Is.EqualTo(2200));
            Assert.That(result.Items[0].Name, Is.EqualTo("iPhone 15"));
            Assert.That(result.Items[0].Subtotal, Is.EqualTo(2000));
        }

        #endregion

        #region AddToCartAsync

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-5)]
        public async Task AddToCartAsync_WhenQuantityIsZeroOrNegative_ReturnsBadRequest(int invalidQuantity)
        {
            // Arrange
            var dto = new AddToCartDto { ProductId = 1, Quantity = invalidQuantity };

            // Act
            var result = await _cartService.AddToCartAsync(1, dto);

            // Assert
            Assert.That(result.Status, Is.EqualTo(ServiceResultStatus.BadRequest));
            Assert.That(result.Message, Is.EqualTo("Quantity must be greater than zero."));
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Test]
        public async Task AddToCartAsync_WhenProductNotFound_ReturnsNotFound()
        {
            // Arrange
            var dto = new AddToCartDto { ProductId = 99, Quantity = 2 };

            _cartRepoMock.Setup(r => r.GetProductByIdAsync(dto.ProductId))
                         .ReturnsAsync((Product?)null);

            // Act
            var result = await _cartService.AddToCartAsync(1, dto);

            // Assert
            Assert.That(result.Status, Is.EqualTo(ServiceResultStatus.NotFound));
            Assert.That(result.Message, Is.EqualTo("Product not found."));
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Test]
        public async Task AddToCartAsync_WhenStockIsInsufficient_ReturnsBadRequest()
        {
            // Arrange
            var dto = new AddToCartDto { ProductId = 1, Quantity = 10 };
            var product = new Product { Id = 1, Stock = 5 };

            _cartRepoMock.Setup(r => r.GetProductByIdAsync(dto.ProductId))
                         .ReturnsAsync(product);

            // Act
            var result = await _cartService.AddToCartAsync(1, dto);

            // Assert
            Assert.That(result.Status, Is.EqualTo(ServiceResultStatus.BadRequest));
            Assert.That(result.Message, Is.EqualTo("Not enough stock."));
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Test]
        public async Task AddToCartAsync_WhenCartDoesNotExist_CreatesNewCartAndAddsItem()
        {
            // Arrange
            int userId = 1;
            var dto = new AddToCartDto { ProductId = 1, Quantity = 2 };
            var product = new Product { Id = 1, Stock = 10 };

            _cartRepoMock.Setup(r => r.GetProductByIdAsync(dto.ProductId)).ReturnsAsync(product);
            _cartRepoMock.Setup(r => r.GetCartByUserIdAsync(userId)).ReturnsAsync((Cart?)null);

            // Act
            var result = await _cartService.AddToCartAsync(userId, dto);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            _cartRepoMock.Verify(r => r.AddCartAsync(It.Is<Cart>(c =>
                c.UserId == userId &&
                c.Items.Count == 1 &&
                c.Items.First().ProductId == 1 &&
                c.Items.First().Quantity == 2)), Times.Once);
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task AddToCartAsync_WhenCartExistsWithoutItem_AddsNewItem()
        {
            // Arrange
            int userId = 1;
            var dto = new AddToCartDto { ProductId = 1, Quantity = 2 };
            var product = new Product { Id = 1, Stock = 10 };
            var existingCart = new Cart { Id = 1, UserId = userId };

            _cartRepoMock.Setup(r => r.GetProductByIdAsync(dto.ProductId)).ReturnsAsync(product);
            _cartRepoMock.Setup(r => r.GetCartByUserIdAsync(userId)).ReturnsAsync(existingCart);

            // Act
            var result = await _cartService.AddToCartAsync(userId, dto);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(existingCart.Items.Count, Is.EqualTo(1));
            Assert.That(existingCart.Items.First().Quantity, Is.EqualTo(2));
            _cartRepoMock.Verify(r => r.AddCartAsync(It.IsAny<Cart>()), Times.Never);
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task AddToCartAsync_WhenItemAlreadyExistsInCart_IncreasesQuantity()
        {
            // Arrange
            int userId = 1;
            var dto = new AddToCartDto { ProductId = 1, Quantity = 2 };
            var product = new Product { Id = 1, Stock = 10 };
            var existingCart = new Cart
            {
                Id = 1,
                UserId = userId,
                Items = new List<CartItem> { new CartItem { ProductId = 1, Quantity = 3 } }
            };

            _cartRepoMock.Setup(r => r.GetProductByIdAsync(dto.ProductId)).ReturnsAsync(product);
            _cartRepoMock.Setup(r => r.GetCartByUserIdAsync(userId)).ReturnsAsync(existingCart);

            // Act
            var result = await _cartService.AddToCartAsync(userId, dto);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(existingCart.Items.First().Quantity, Is.EqualTo(5));
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task AddToCartAsync_WhenExistingItemExceedsStock_ReturnsBadRequest()
        {
            // Arrange
            int userId = 1;
            var dto = new AddToCartDto { ProductId = 1, Quantity = 3 };
            var product = new Product { Id = 1, Stock = 5 };
            var existingCart = new Cart
            {
                Id = 1,
                UserId = userId,
                Items = new List<CartItem> { new CartItem { ProductId = 1, Quantity = 4 } }
            };

            _cartRepoMock.Setup(r => r.GetProductByIdAsync(dto.ProductId)).ReturnsAsync(product);
            _cartRepoMock.Setup(r => r.GetCartByUserIdAsync(userId)).ReturnsAsync(existingCart);

            // Act (4 + 3 = 7 > 5)
            var result = await _cartService.AddToCartAsync(userId, dto);

            // Assert
            Assert.That(result.Status, Is.EqualTo(ServiceResultStatus.BadRequest));
            Assert.That(result.Message, Is.EqualTo("Not enough stock."));
            Assert.That(existingCart.Items.First().Quantity, Is.EqualTo(4)); // dəyişməməlidir
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        #endregion

        #region UpdateQuantityAsync

        [TestCase(0)]
        [TestCase(-3)]
        public async Task UpdateQuantityAsync_WhenQuantityIsZeroOrNegative_ReturnsBadRequest(int quantity)
        {
            // Act
            var result = await _cartService.UpdateQuantityAsync(1, 10, quantity);

            // Assert
            Assert.That(result.Status, Is.EqualTo(ServiceResultStatus.BadRequest));
            Assert.That(result.Message, Is.EqualTo("Quantity must be greater than zero."));
            _cartRepoMock.Verify(r => r.GetCartItemAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Test]
        public async Task UpdateQuantityAsync_WhenCartItemNotFound_ReturnsNotFound()
        {
            // Arrange
            int userId = 1, productId = 10;
            _cartRepoMock.Setup(r => r.GetCartItemAsync(userId, productId))
                         .ReturnsAsync((CartItem?)null);

            // Act
            var result = await _cartService.UpdateQuantityAsync(userId, productId, 3);

            // Assert
            Assert.That(result.Status, Is.EqualTo(ServiceResultStatus.NotFound));
            Assert.That(result.Message, Is.EqualTo("Cart item not found."));
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Test]
        public async Task UpdateQuantityAsync_WhenQuantityExceedsStock_ReturnsBadRequest()
        {
            // Arrange
            int userId = 1, productId = 10;
            var cartItem = new CartItem
            {
                ProductId = productId,
                Quantity = 1,
                Product = new Product { Id = productId, Stock = 5 }
            };

            _cartRepoMock.Setup(r => r.GetCartItemAsync(userId, productId))
                         .ReturnsAsync(cartItem);

            // Act
            var result = await _cartService.UpdateQuantityAsync(userId, productId, 10);

            // Assert
            Assert.That(result.Status, Is.EqualTo(ServiceResultStatus.BadRequest));
            Assert.That(result.Message, Is.EqualTo("Not enough stock."));
            Assert.That(cartItem.Quantity, Is.EqualTo(1));
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Test]
        public async Task UpdateQuantityAsync_WhenValidQuantity_UpdatesAndSaves()
        {
            // Arrange
            int userId = 1, productId = 10;
            var cartItem = new CartItem
            {
                ProductId = productId,
                Quantity = 1,
                Product = new Product { Id = productId, Stock = 10 }
            };

            _cartRepoMock.Setup(r => r.GetCartItemAsync(userId, productId))
                         .ReturnsAsync(cartItem);

            // Act
            var result = await _cartService.UpdateQuantityAsync(userId, productId, 4);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(cartItem.Quantity, Is.EqualTo(4));
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region RemoveFromCartAsync

        [Test]
        public async Task RemoveFromCartAsync_WhenCartItemNotFound_ReturnsNotFound()
        {
            // Arrange
            int userId = 1, productId = 5;
            _cartRepoMock.Setup(r => r.GetCartItemAsync(userId, productId))
                         .ReturnsAsync((CartItem?)null);

            // Act
            var result = await _cartService.RemoveFromCartAsync(userId, productId);

            // Assert
            Assert.That(result.Status, Is.EqualTo(ServiceResultStatus.NotFound));
            Assert.That(result.Message, Is.EqualTo("Cart item not found."));
            _cartRepoMock.Verify(r => r.RemoveCartItem(It.IsAny<CartItem>()), Times.Never);
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Test]
        public async Task RemoveFromCartAsync_WhenCartItemExists_RemovesAndSaves()
        {
            // Arrange
            int userId = 1, productId = 5;
            var cartItem = new CartItem { Id = 12, ProductId = productId };

            _cartRepoMock.Setup(r => r.GetCartItemAsync(userId, productId))
                         .ReturnsAsync(cartItem);

            // Act
            var result = await _cartService.RemoveFromCartAsync(userId, productId);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            _cartRepoMock.Verify(r => r.RemoveCartItem(cartItem), Times.Once);
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region ClearCartAsync

        [Test]
        public async Task ClearCartAsync_WhenCartDoesNotExist_DoesNothing()
        {
            // Arrange
            _cartRepoMock.Setup(r => r.GetCartByUserIdAsync(1))
                         .ReturnsAsync((Cart?)null);

            // Act
            await _cartService.ClearCartAsync(1);

            // Assert
            _cartRepoMock.Verify(r => r.RemoveCartItems(It.IsAny<IEnumerable<CartItem>>()), Times.Never);
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Test]
        public async Task ClearCartAsync_WhenCartExists_RemovesAllItemsAndSaves()
        {
            // Arrange
            int userId = 1;
            var cart = new Cart
            {
                Id = 5,
                UserId = userId,
                Items = new List<CartItem>
                {
                    new CartItem { Id = 1, ProductId = 10 },
                    new CartItem { Id = 2, ProductId = 11 }
                }
            };

            _cartRepoMock.Setup(r => r.GetCartByUserIdAsync(userId))
                         .ReturnsAsync(cart);

            // Act
            await _cartService.ClearCartAsync(userId);

            // Assert
            _cartRepoMock.Verify(
                r => r.RemoveCartItems(It.Is<IEnumerable<CartItem>>(i => i.Count() == 2)),
                Times.Once);
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion
    }
}