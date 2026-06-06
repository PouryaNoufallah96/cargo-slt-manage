namespace Utilities.DTOs
{
    public class Pagination
    {
        private int _page = 1;
        private int _size = 25;
        public int Page
        {
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }
        public int Size
        {
            get => _size;
            set => _size = value < 1 ? 25 : value > 100 ? 100 : value;
        }
    }
}
